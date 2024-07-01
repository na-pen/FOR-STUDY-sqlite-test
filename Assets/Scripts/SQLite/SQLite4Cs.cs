using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections;
using Unity.VisualScripting;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using SQLite4Cs;
using UnityEditor.Experimental.Rendering;
using UnityEditor.MemoryProfiler;
using System.Runtime.CompilerServices;
using UnityEditor.Search;
using System.Data.Common;
using System.Diagnostics;


namespace SQLite4Cs
{
    public class SQLite
    {

        public void Open(string pass)
        {
            if (SQLiteDLL.sqlite3_open(pass, out _db) != 0)
            {
                //Console.WriteLine("Failed to open database");
                return;
            }
            else
            {
                //Console.WriteLine("データベースへの接続を開始しました");
            }
            return;
        }

        public void Close()
        {
            //データベースへの接続の終了
            SQLiteDLL.sqlite3_close(_db);
            //Console.WriteLine("データベースへの接続を終了しました");
            return;
        }

        public void Create<T>() where T : Database, new()
        {
            T obj = new();
            obj.Parser(obj);


            string query = $"CREATE TABLE {obj.TableName} (";

            for (int i = 0; i < obj.ColumnName.Count; i++)
            {
                query += obj.ColumnName[i];

                switch (obj.ColumnType[i])
                {
                    case Type t when t == typeof(int):
                        query += " integer ";
                        break;

                    case Type t when t == typeof(double):
                        query += " real ";
                        break;

                    case Type t when t == typeof(float):
                        query += " real ";
                        break;

                    case Type t when t == typeof(string):
                        query += " text ";
                        break;
                }

                foreach (var attribute in obj.ColumnAttributes[i])
                {
                    switch (attribute)
                    {
                        case Type a when a == typeof(AutoIncrementAttribute):
                            query += "autoincrement ";
                            break;

                        case Type a when a == typeof(PrimaryKeyAttribute):
                            query += "primary key ";
                            break;

                        case Type a when a == typeof(NotNullAttribute):
                            query += "not null ";
                            break;

                        case Type a when a == typeof(UniqueAttribute):
                            query += "unique ";
                            break;

                        default:
                            break;
                    }
                }
                if (i < obj.ColumnName.Count - 1)
                    query += ", ";
            }
            query += ")";
            //Console.WriteLine(query);
            ExecuteQuery(query);

            return;
        }

        public int Insert<T>(T[] obj) where T : Database, new()
        {
            foreach (T a in obj)
            {
                a.Parser(a);
            }

            string query = $"INSERT INTO {obj[0].TableName}";
            string queryColumns = null;
            List<string> columns = new List<string>();
            string queryValue = "VALUES ";
            List<string> values = new List<string>();

            queryColumns = "( ";
            for (int i = 0; i < obj[0].ColumnName.Count; i++)
            {
                bool isAuto = Array.IndexOf(obj[0].ColumnAttributes[i], typeof(AutoIncrementAttribute)) != -1;
                if (!isAuto)
                {
                    queryColumns += obj[0].ColumnName[i];
                    columns.Add(obj[0].ColumnName[i]);
                    if (i != obj[0].ColumnName.Count - 1)
                        queryColumns += ",";
                }
            }
            queryColumns += " )";

            foreach (var o in obj.Select((value, i) => (value, i)))
            {
                queryValue += "(";
                for (int i = 0; i < o.value.ColumnName.Count; i++)
                {
                    bool isAuto = Array.IndexOf(o.value.ColumnAttributes[i], typeof(AutoIncrementAttribute)) != -1;
                    if (!isAuto)
                    {
                        values.Add(o.value.TableValue[i].ToString());
                        queryValue += " ? ";
                        if (i != o.value.ColumnName.Count - 1)
                            queryValue += ", ";
                    }

                    if (i == o.value.ColumnName.Count - 1)
                    {
                        queryValue += ") ";
                    }

                }
                if (o.i != obj.Length - 1)
                    queryValue += ", ";
            }
            IntPtr insertStmt;
            int p;

            string insertQuery = $"{query} {queryColumns} {queryValue}";
            (p, insertStmt) = Prepare(_db, insertQuery, out insertStmt);

            // バインドする値をShift_JISからUTF-8に変換してバイト配列にする
            for (int i = 0; i < values.Count; i++)
            {
                //System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // memo: Shift-JISを扱うためのおまじない
                byte[] valueBytes = Encoding.Convert(Encoding.GetEncoding("Shift_JIS"), Encoding.UTF8, Encoding.GetEncoding("Shift_JIS").GetBytes(values[i]));
                SQLiteDLL.sqlite3_bind_text(insertStmt, i + 1, valueBytes, valueBytes.Length, IntPtr.Zero);
            }

            if (SQLiteDLL.sqlite3_step(insertStmt) != 101 /* SQLITE_DONE */)
            {
                Console.WriteLine("Failed to execute insert statement.");
                Console.WriteLine(Marshal.PtrToStringUTF8(SQLiteDLL.sqlite3_errmsg(_db)));
            }
            else
            {
                Console.WriteLine("Insert succeeded.");
            }

            // ステートメントを終了し、データベースを閉じる
            SQLiteDLL.sqlite3_finalize(insertStmt);

            return 0;
        }


        public TableOperation<T> Table<T>() where T : Database, new()
        {
            return new TableOperation<T>();
        }

        public class TableOperation<T> where T : Database, new()
        {
            string selectFromQuery = null;
            string whereQuery = null;
            string updateQuery = null;
            string setQuery = null;

            string[] selectColumns = null;
            private SQLite _sqlite = new();
            //IntPtr _db;

            public TableOperation()
            {
                //_db = db;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="resultTable"></param>
            /// <param name="selectColumn">抽出するカラム名を指定します。大文字、小文字は識別されます。</param>
            /// <returns></returns>
            /// 
            public TableOperation<T> Select(string[] selectColumn = null)
            {
                T resultTable = new();
                resultTable.Parser(resultTable);

                if (selectColumn != null)
                {

                    bool allElementsInListB = selectColumn.All(element => resultTable.ColumnName.Contains(element));
                    bool noDuplicatesInArrayA = selectColumn.Distinct().Count() == selectColumn.Length;
                    if (!allElementsInListB)
                        throw new ArgumentException("The specified column name does not exist in the database.");
                    else if (!noDuplicatesInArrayA)
                        throw new ArgumentException("There is a duplicate in the specified column name.");
                    selectColumns = selectColumn;
                }
                else
                {
                    selectColumn ??= new string[] { "*" };
                    T temp = new();
                    temp.Parser(temp);
                    selectColumns = temp.ColumnName.ToArray();
                }
                selectFromQuery = $"SELECT {String.Join(", ", selectColumn)} FROM {resultTable.GetType().Name}";

                return this;
            }

            /*-------------- Where関連 --------------*/

            List<string> whereValues = new List<string>();
            public TableOperation<T> Where(Expression<Func<T, bool>> predicate = null)
            {
                if (predicate != null)
                {
                    whereQuery = $" WHERE {QueryConv(predicate.Body)} ";
                }
                else
                {
                    whereQuery = $" WHERE ";
                }
                return this;
            }
            public TableOperation<T> Update()
            {
                T table = new();
                table.Parser(table);
                updateQuery = $" UPDATE {table.TableName} ";
                return this;
            }

            public TableOperation<T> Set(Expression<Func<T, bool>> predicate)
            {
                T temp = new();
                temp.Parser(temp);
                selectColumns = temp.ColumnName.ToArray();

                setQuery += $" SET {QueryConv(predicate.Body)} ";
                return this;
            }

            public TableOperation<T> Set(Expression<Func<T, bool>>[] predicates)
            {
                T temp = new();
                temp.Parser(temp);
                selectColumns = temp.ColumnName.ToArray();

                setQuery += " SET";
                foreach (var predicate in predicates.Select((value, i) => (value, i)))
                {
                    setQuery += $" {QueryConv(predicate.value.Body)} ";
                    if (predicates.Length - 1 > predicate.i)
                    {
                        setQuery += ",";
                    }
                }
                return this;
            }

            public TableOperation<T> Like(string columnName, string pattern)
            {
                whereQuery += $"{columnName} LIKE {pattern}";
                return this;
            }

            public TableOperation<T> LikeAnd(string columnName, List<string> strs, bool NisNot = true)
            {
                foreach (var str in strs.Select((value, i) => (value, i)))
                {
                    if (!NisNot)
                    {
                        whereQuery += $"{columnName} NOT LIKE {str.value}";
                    }
                    else
                        whereQuery += $"{columnName} LIKE {str.value}";
                    if (str.i < strs.Count - 1)
                        whereQuery += " AND ";
                }
                return this;
            }

            public TableOperation<T> And(Expression<Func<T, bool>> predicate = null)
            {
                if (predicate != null)
                {
                    whereQuery += $" AND {QueryConv(predicate.Body)} ";
                }
                else
                {
                    whereQuery += " AND ";
                }
                return this;
            }

            public TableOperation<T> Or(Expression<Func<T, bool>> predicate)
            {
                whereQuery += $" OR {QueryConv(predicate.Body)} ";
                return this;
            }

            public TableOperation<T> Not(Expression<Func<T, bool>> predicate)
            {
                whereQuery += $" NOT {QueryConv(predicate.Body)} ";
                return this;
            }

            public List<T> Do()
            {
                List<T> result;
                string q;
                if (updateQuery == null)
                {
                    q = selectFromQuery + whereQuery;
                    result = _sqlite.ExecuteSelectQuery<T>(selectFromQuery + whereQuery, selectColumns, whereValues);
                }
                else
                {
                    q = selectFromQuery + updateQuery + setQuery + whereQuery;
                    result = _sqlite.ExecuteSelectQuery<T>(selectFromQuery + updateQuery + setQuery + whereQuery, selectColumns, whereValues);
                }

                //Console.WriteLine(selectFromQuery + whereQuery);
                return result;
            }

            public List<T> Do(string query, string[] selectColumns)
            {
                List<T> result;
                result = _sqlite.ExecuteSelectQuery<T>(query, selectColumns, whereValues);

                //Console.WriteLine(selectFromQuery + whereQuery);
                return result;
            }

            public (string, string[], List<string>) GetQuery()
            {
                //var a = selectFromQuery + updateQuery + setQuery + whereQuery;
                return (selectFromQuery + updateQuery + setQuery + whereQuery, selectColumns, whereValues);
            }

            private string QueryConv(Expression predicate)
            {
                string returnQuery = null;
                LambdaExpression lambdaExp = Expression.Lambda(predicate);

                if (predicate == null) { }
                else if (predicate is BinaryExpression)
                {
                    StackTrace stackTrace = new StackTrace();

                    // 呼び出し元のメソッドのスタックフレームを取得
                    StackFrame frame = stackTrace.GetFrame(1);

                    // 呼び出し元のメソッドを取得
                    var method = frame.GetMethod().Name;


                    BinaryExpression binary = predicate as BinaryExpression;
                    var left = QueryConv(binary.Left);
                    var right = QueryConv(binary.Right);
                    switch (binary.NodeType)
                    {
                        case (ExpressionType.Equal):
                            returnQuery = $"{left} == {right}";
                            break;

                        case (ExpressionType.NotEqual):
                            returnQuery = $"{left} != {right}";
                            break;

                        case (ExpressionType.LessThan):
                            returnQuery = $"{left} < {right}";
                            break;

                        case (ExpressionType.GreaterThan):
                            returnQuery = $"{left} > {right}";
                            break;

                        case (ExpressionType.LessThanOrEqual):
                            returnQuery = $"{left} <= {right}";
                            break;

                        case (ExpressionType.GreaterThanOrEqual):
                            returnQuery = $"{left} >= {right}";
                            break;

                        default:
                            throw new ArgumentException("Unparsable arguments were used.");

                    }
                    if (method != "Set")
                    {
                        returnQuery = $"({returnQuery})";
                    }
                }
                else
                {

                    if (ExpressionType.Call == lambdaExp.Body.NodeType)
                    {
                        MethodCallExpression call = lambdaExp.Body as MethodCallExpression;
                        switch (call.Method.Name)
                        {
                            //Whereで(x => ids.Contains(x.ID)等をしたときに当たる
                            case "Contains":
                                returnQuery = $"{GetDeepestName(call)} IN ({QueryConv(call.Object)})";
                                break;


                        }
                    }

                    else if (lambdaExp.Body.NodeType == ExpressionType.MemberAccess)
                    {
                        MemberExpression memberAccess = lambdaExp.Body as MemberExpression;
                        switch (memberAccess.Expression.NodeType)
                        {
                            case ExpressionType.Constant:
                                Func<object> compiledLambda = null;
                                try
                                {
                                    compiledLambda = Expression.Lambda<Func<object>>(memberAccess).Compile();
                                }
                                catch (ArgumentException)
                                {
                                    Expression converted = Expression.Convert(memberAccess, typeof(object));

                                    // object型でコンパイル
                                    compiledLambda = Expression.Lambda<Func<object>>(converted).Compile();
                                }

                                object obj = compiledLambda();
                                if (obj is IEnumerable enumerable && !(obj is string))
                                {
                                    // 配列またはリストの場合、要素をカンマ区切りで結合する
                                    returnQuery = string.Join(", ", enumerable.Cast<object>());
                                    foreach (var item in enumerable.Cast<object>().Select((value, i) => (value, i)))
                                    {
                                        returnQuery += " ? ";
                                        whereValues.Add(item.value.ToString());
                                        if (enumerable.Cast<object>().Count() > item.i)
                                        {
                                            returnQuery += ",";
                                        }
                                    }
                                }
                                else
                                {
                                    // それ以外の場合は、その値をそのまま文字列にする
                                    Type type = obj.GetType();
                                    switch (type)
                                    {
                                        case Type t when t == typeof(string):
                                            if (!(obj.ToString().ToString().StartsWith("\"") && obj.ToString().ToString().EndsWith("\"")))
                                            {
                                                whereValues.Add(obj.ToString());
                                                returnQuery = " ? ";
                                            }
                                            else
                                            {
                                                whereValues.Add(obj.ToString());
                                                returnQuery = " ? ";
                                            }
                                            break;

                                        default:
                                            Type type0 = obj.GetType();
                                            returnQuery = " ? ";
                                            if (type0.Namespace.StartsWith("System"))
                                            {
                                                whereValues.Add(obj.ToString());
                                            }
                                            break;
                                    }

                                }
                                break;

                            case ExpressionType.Parameter:
                                returnQuery = memberAccess.ToString().Replace($"{memberAccess.Expression.ToString()}.", "");
                                break;

                            case ExpressionType.MemberAccess:
                                returnQuery = QueryConv(memberAccess.Expression);
                                break;

                            default:
                                var temp = memberAccess.Expression.NodeType;
                                break;

                        }

                    }

                    else if (lambdaExp.Body.NodeType == ExpressionType.Constant)
                    {
                        Type type = lambdaExp.Body.Type;
                        switch (type)
                        {
                            case Type t when t == typeof(string):
                                returnQuery = " ? ";
                                whereValues.Add(lambdaExp.Body.ToString());
                                break;

                            default:
                                returnQuery = " ? ";
                                whereValues.Add(lambdaExp.Body.ToString());
                                break;
                        }

                    }
                }


                return returnQuery;
            }

            private static string GetDeepestName(Expression expression)
            {
                switch (expression)
                {
                    case MemberExpression memberExpression:
                        return memberExpression.Member.Name;

                    case MethodCallExpression methodCallExpression:
                        // Argumentsを再帰的に処理
                        foreach (var argument in methodCallExpression.Arguments)
                        {
                            var name = GetDeepestName(argument);
                            if (!string.IsNullOrEmpty(name))
                            {
                                return name;
                            }
                        }
                        return methodCallExpression.Method.Name;

                    default:
                        return null;
                }
            }
        }
        //データベースの接続を表すポインタ
        private static IntPtr _db;

        /*-------------- SQL実行関連 --------------*/
        private int ExecuteQuery(string query)
        {
            IntPtr errMsg;
            var temp = SQLiteDLL.sqlite3_exec(_db, query, IntPtr.Zero, IntPtr.Zero, out errMsg);
            if (temp != 0)
            {
                string error = Marshal.PtrToStringAnsi(errMsg);
                Console.WriteLine("SQLite error: " + error);
                return temp;
            }
            query = "";
            return temp;
        }

        private List<T> ExecuteSelectQuery<T>(string query, string[] selectColumns, List<string> values) where T : Database, new()
        {
            List<T> list = new List<T>();
            if (selectColumns == null)
            {
                T temp = new();
                temp.Parser(temp);
                selectColumns = temp.ColumnName.ToArray();
            }

            IntPtr stmt;
            int p;
            (p, stmt) = Prepare(_db, query, out stmt);
            if (p != 0)
            {
                Console.WriteLine(Marshal.PtrToStringUTF8(SQLiteDLL.sqlite3_errmsg(_db)));
                throw new ArgumentException($"Failed to prepare statement {query}");
            }

            // 値をShift_JISからUTF-8に変換してバイト配列にしてバインドする
            for (int i = 0; i < values.Count; i++)
            {
                //System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // memo: Shift-JISを扱うためのおまじない
                byte[] valueBytes = Encoding.Convert(Encoding.GetEncoding("Shift_JIS"), Encoding.UTF8, Encoding.GetEncoding("Shift_JIS").GetBytes(values[i]));
                SQLiteDLL.sqlite3_bind_text(stmt, i + 1, valueBytes, valueBytes.Length, IntPtr.Zero);
            }


            int columnCount = SQLiteDLL.sqlite3_column_count(stmt);

            int c = 0;
            Dictionary<string, Type> tableInfo = GetTableInfo<T>();

            while (SQLiteDLL.sqlite3_step(stmt) == 100) // 継続中 = 100,終了 = 101
            {
                T row = new();
                for (int i = 0; i < columnCount; i++)
                {
                    switch (tableInfo[selectColumns[i]])
                    {
                        case Type t when t == typeof(int):
                            AssignValuesToPropertie(row, selectColumns[i], SQLiteDLL.sqlite3_column_int(stmt, i));
                            break;

                        case Type t when t == typeof(string):
                            IntPtr textPtr = SQLiteDLL.sqlite3_column_text(stmt, i);
                            if (textPtr != IntPtr.Zero)
                            {
                                //System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                                byte[] utf8Bytes = Encoding.UTF8.GetBytes(Marshal.PtrToStringUTF8(textPtr));

                                // UTF-8のバイト配列からShift_JISのバイト配列に変換
                                byte[] shiftJisBytes = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding("Shift_JIS"), utf8Bytes);

                                // Shift_JISのバイト配列を文字列に変換
                                string shiftJisString = Encoding.GetEncoding("Shift_JIS").GetString(shiftJisBytes);

                                AssignValuesToPropertie(row, selectColumns[i], shiftJisString);
                            }
                            else
                            {
                                AssignValuesToPropertie(row, selectColumns[i], "NULL");
                            }
                            break;

                        case Type t when t == typeof(double):
                            AssignValuesToPropertie(row, selectColumns[i], SQLiteDLL.sqlite3_column_double(stmt, i));
                            break;

                    }
                }
                c++;
                list.Add(row);
            }

            SQLiteDLL.sqlite3_finalize(stmt);


            return list;
        }
        private static void AssignValuesToPropertie(object target, string propertyName, object value)
        {
            Type type = target.GetType();

            PropertyInfo property = type.GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
            }

        }
        private static void AddElementToList<A>(IList list, A element)
        {
            if (list is List<A> genericList)
            {
                genericList.Add(element);
            }
            else
            {
                throw new ArgumentException($"The list is not of type List<{typeof(A).Name}>");
            }
        }

        private static (int, IntPtr) Prepare(IntPtr db, string query, out IntPtr stmt)
        {
            IntPtr queryPtr = IntPtr.Zero;
            try
            {
                byte[] queryBytes = System.Text.Encoding.UTF8.GetBytes(query);
                queryPtr = Marshal.AllocHGlobal(queryBytes.Length + 1);
                Marshal.Copy(queryBytes, 0, queryPtr, queryBytes.Length);
                Marshal.WriteByte(queryPtr, queryBytes.Length, 0);

                return (SQLiteDLL.sqlite3_prepare_v2(db, queryPtr, -1, out stmt, IntPtr.Zero), stmt);
            }
            finally
            {
                if (queryPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(queryPtr);
                }
            }
        }

        private Dictionary<string, Type> GetTableInfo<T>() where T : Database, new()
        {
            T obj = new();
            obj.Parser(obj);

            IntPtr stmt;
            int p;
            string query = $"PRAGMA table_info({obj.TableName});";
            Dictionary<string, Type> result = new Dictionary<string, Type>();
            (p, stmt) = Prepare(_db, query, out stmt);
            if (p != 0)
            {
                throw new ArgumentException($"Failed to prepare statement {query}");
            }
            while (SQLiteDLL.sqlite3_step(stmt) == 100)
            {
                string columnName = Marshal.PtrToStringUTF8(SQLiteDLL.sqlite3_column_text(stmt, 1));
                Type columnType = typeof(string);
                var a = Marshal.PtrToStringUTF8(SQLiteDLL.sqlite3_column_text(stmt, 2));
                switch (Marshal.PtrToStringUTF8(SQLiteDLL.sqlite3_column_text(stmt, 2)))
                {
                    case "INTEGER":
                        columnType = typeof(int);
                        break;

                    case "REAL":
                        columnType = typeof(double);
                        break;

                    case "TEXT":
                        columnType = typeof(string);
                        break;

                    default:
                        break;
                }
                result.Add(columnName, columnType);
            }

            // ステートメントを終了し、データベースを閉じる
            SQLiteDLL.sqlite3_finalize(stmt);
            return result;
        }
    }

    internal class SQLiteDLL
    {
        /* SQLiteのDLL接続関係の設定 */
        [DllImport("sqlite3", EntryPoint = "sqlite3_open")]
        internal static extern int sqlite3_open(string filename, out IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_close")]
        internal static extern int sqlite3_close(IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_prepare_v2(IntPtr db, IntPtr zSql, int nByte, out IntPtr ppStmpt, IntPtr pzTail);


        [DllImport("sqlite3", EntryPoint = "sqlite3_step")]
        internal static extern int sqlite3_step(IntPtr stmHandle);

        [DllImport("sqlite3", EntryPoint = "sqlite3_finalize")]
        internal static extern int sqlite3_finalize(IntPtr stmHandle);

        [DllImport("sqlite3", EntryPoint = "sqlite3_errmsg")]
        internal static extern IntPtr sqlite3_errmsg(IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_count")]
        internal static extern int sqlite3_column_count(IntPtr stmHandle);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_name")]
        internal static extern IntPtr sqlite3_column_name(IntPtr stmHandle, int iCol);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_type")]
        internal static extern int sqlite3_column_type(IntPtr stmHandle, int iCol);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_int")]
        internal static extern int sqlite3_column_int(IntPtr stmHandle, int iCol);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_double")]
        internal static extern double sqlite3_column_double(IntPtr stmHandle, int iCol);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_blob")]
        internal static extern IntPtr sqlite3_column_blob(IntPtr stmHandle, int iCol);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_bytes")]
        internal static extern int sqlite3_column_bytes(IntPtr stmHandle, int iCol);

        [DllImport("sqlite3", EntryPoint = "sqlite3_exec")]
        internal static extern int sqlite3_exec(IntPtr db, string sql, IntPtr callback, IntPtr args, out IntPtr errMsg);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_bind_text(IntPtr stmt, int index, byte[] value, int n, IntPtr free);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_text")]
        internal static extern IntPtr sqlite3_column_text(IntPtr stmHandle, int iCol);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_bind_parameter_index(IntPtr stmt, string zName);


    }

    public class Database
    {
        internal string TableName = null;
        internal List<string> ColumnName = new();
        internal List<Type> ColumnType = new();
        internal List<Type[]> ColumnAttributes = new();
        internal List<object> TableValue = new();


        internal void Parser<T>(T obj) where T : Database
        {
            Type t = obj.GetType();
            PropertyInfo[] properties = t.GetProperties();

            foreach (var property in properties.Select((value, i) => (value, i)))
            {
                object _value = property.value.GetValue(obj);
                if (_value == null)
                {
                    TableValue.Add("NULL");
                }
                else
                {

                    switch (property.value.PropertyType)
                    {
                        case Type v when v == typeof(int):
                            TableValue.Add(_value.ToString());
                            break;

                        case Type v when v == typeof(double):
                            TableValue.Add(_value.ToString());
                            break;

                        case Type v when v == typeof(float):
                            TableValue.Add(_value.ToString());
                            break;

                        case Type v when v == typeof(string):
                            TableValue.Add(_value);
                            break;

                    }
                }

                object[] attributes = property.value.GetCustomAttributes(true);
                ColumnAttributes.Add(attributes.Select(element => element.GetType()).ToArray());
                ColumnName.Add(property.value.Name);
                ColumnType.Add(property.value.PropertyType);
            }
            TableName = t.Name;
        }
    }



    /// <summary>値を自動生成する属性 </summary>
    public class AutoIncrementAttribute : Attribute { }

    /// <summary>カラムをプライマリーキーに設定する属性 </summary>
    public class PrimaryKeyAttribute : Attribute { }

    /// <summary>nullを許可しない属性</summary>
    public class NotNullAttribute : Attribute { }

    /// <summary>重複を許可しない属性</summary>
    public class UniqueAttribute : Attribute { }
}


#if DEBUG
/// <summary>
/// 例えば、"TestTable" というテーブル、列名(型) "ID(str),Value(int)" をもつデータベースの場合、このクラスのようにする必要があります 
/// 
/// </summary>
public class TestTable : Database
{
    [AutoIncrement, PrimaryKey]
    public int ID { get; set; }
    public string Value { get; set; }

    public string[] ColumnList()
    {
        return new string[0];
    }
}
#endif
