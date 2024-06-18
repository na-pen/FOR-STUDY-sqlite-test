using System;
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


namespace SQLite4Cs
{
    public class SQLite
    {

        public void Open(string pass)
        {
            if (SQLiteDLL.sqlite3_open(pass, out _db) != 0)
            {
                Debug.LogError("Failed to open database");
                return;
            }
            else
            {
                Debug.Log("データベースへの接続を開始しました");
            }
            return;
        }

        public void Close()
        {
            //データベースへの接続の終了
            SQLiteDLL.sqlite3_close(_db);
            Debug.Log("データベースへの接続を終了しました");
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
                    }
                }
                if (i < obj.ColumnName.Count - 1)
                    query += ", ";
            }
            query += ")";
            Debug.Log(query);
            ExecuteQuery(query);

            return;
        }


        public void Insert<T>(T[] obj) where T : Database, new()
        {
            foreach (T a in obj)
            {
                a.Parser(a);
            }

            string query = $"INSERT INTO {obj[0].TableName}";
            string queryColumns = $"( {String.Join(',', obj[0].ColumnName)} )";
            string queryValue = $"VALUES {string.Join(",", obj.Select(o => $"( {string.Join(',', o.TableValue)} )"))}";
            query = $"{query} {queryColumns} {queryValue}";
            Debug.Log(query);
            ExecuteQuery(query);

            return;
        }


        public TableOperation<T> Table<T>() where T : Database, new()
        {
            return new TableOperation<T>();
        }

        public class TableOperation<T> where T : Database, new()
        {
            string selectFromQuery = null;
            //string query = null;
            string whereQuery = null;
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
                selectColumn ??= new string[] { "*" };
                resultTable.Parser(resultTable);

                bool allElementsInListB = selectColumn.All(element => resultTable.ColumnName.Contains(element));
                bool noDuplicatesInArrayA = selectColumn.Distinct().Count() == selectColumn.Length;

                selectFromQuery = $"SELECT {String.Join(", ", selectColumn)} FROM {resultTable.GetType().Name}";

                if (!allElementsInListB)
                    throw new ArgumentException("The specified column name does not exist in the database.");
                else if (!noDuplicatesInArrayA)
                    throw new ArgumentException("There is a duplicate in the specified column name.");

                selectColumns = selectColumn;
                return this;
            }

            /*-------------- Where関連 --------------*/
            public TableOperation<T> Where(Expression<Func<T, bool>> predicate)
            {
                whereQuery = $" WHERE {QueryConv(predicate.Body)} ";
                return this;
            }

            public TableOperation<T> And(Expression<Func<T, bool>> predicate)
            {
                whereQuery += $" AND {QueryConv(predicate.Body)} ";
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

            public IList[] Do()
            {
                IList[] result = _sqlite.ExecuteSelectQuery<T>(selectFromQuery + whereQuery,selectColumns);
                Debug.Log(selectFromQuery + whereQuery);
                return result;
            }
            private string QueryConv(Expression predicate)
            {
                string returnQuery = null;
                LambdaExpression lambdaExp = Expression.Lambda(predicate);

                if (predicate == null) { }
                else if (predicate is BinaryExpression)
                {
                    BinaryExpression binary = predicate as BinaryExpression;
                    var left = QueryConv(binary.Left);
                    var right = QueryConv(binary.Right);
                    switch (binary.NodeType)
                    {
                        case (ExpressionType.Equal):
                            returnQuery = $"({left} == {right})";
                            break;

                        case (ExpressionType.NotEqual):
                            returnQuery = $"({left} != {right}";
                            break;

                        case (ExpressionType.LessThan):
                            returnQuery = $"({left} < {right})";
                            break;

                        case (ExpressionType.GreaterThan):
                            returnQuery = $"({left} > {right})";
                            break;

                        case (ExpressionType.LessThanOrEqual):
                            returnQuery = $"({left} <= {right})";
                            break;

                        case (ExpressionType.GreaterThanOrEqual):
                            returnQuery = $"{left} >= {right}";
                            break;

                        default:
                            throw new ArgumentException("Unparsable arguments were used.");

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
                    //Whereで(x => ids.Contains(x.ID)等をしたときに当たる
                    else if (lambdaExp.Body.NodeType == ExpressionType.MemberAccess)
                    {
                        MemberExpression memberAccess = lambdaExp.Body as MemberExpression;
                        switch (memberAccess.Expression.NodeType)
                        {
                            case ExpressionType.Constant:
                                Func<object> compiledLambda = Expression.Lambda<Func<object>>(memberAccess).Compile();
                                object obj = compiledLambda();
                                if (obj is IEnumerable enumerable && !(obj is string))
                                {
                                    // 配列またはリストの場合、要素をカンマ区切りで結合する
                                    returnQuery = string.Join(", ", enumerable.Cast<object>());
                                }
                                else
                                {
                                    // それ以外の場合は、その値をそのまま文字列にする
                                    returnQuery = obj.ToString();

                                }
                                break;

                            case ExpressionType.Parameter:
                                returnQuery = memberAccess.ToSafeString().Replace($"{memberAccess.Expression.ToString()}.", "");
                                break;
                        }

                    }

                    else if (lambdaExp.Body.NodeType == ExpressionType.Constant)
                    {
                        returnQuery = lambdaExp.Body.ToString();
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
        private void ExecuteQuery(string query)
        {
            IntPtr errMsg;
            var temp = SQLiteDLL.sqlite3_exec(_db, query, IntPtr.Zero, IntPtr.Zero, out errMsg);
            if (temp != 0)
            {
                string error = Marshal.PtrToStringAnsi(errMsg);
                Debug.LogError("SQLite error: " + error);
            }
            query = "";
        }

        private IList[] ExecuteSelectQuery<T>(string query,string[] selectColumns) where T : Database, new()
        {
            IntPtr stmt;
            int p;
            (p,stmt) = Prepare_v2(_db, query, out stmt);
            if (p != 0)
            {
                throw new ArgumentException($"Failed to prepare statement {query}");
            }
            int columnCount = SQLiteDLL.sqlite3_column_count(stmt);

            int c = 0;
            Dictionary<string, Type> tableInfo = GetTableInfo<T>();
            IList[] resultList = new IList[columnCount];

            for (int i = 0; i < columnCount; i++)
            {
                Type listType = typeof(List<>).MakeGenericType(tableInfo[selectColumns[i]]);
                resultList[i] = (IList)Activator.CreateInstance(listType);
            }


            while (SQLiteDLL.sqlite3_step(stmt) == 100) // 継続中 = 100,終了 = 101
            {
                
                for (int i = 0; i < columnCount; i++)
                {
                    
                    
                    
                    switch (tableInfo[selectColumns[i]])
                    {
                        case Type t when t == typeof(int):
                            //resultList[i].Add(SQLiteDLL.sqlite3_column_int(stmt, i));
                            AddElementToList<int>(resultList[i], SQLiteDLL.sqlite3_column_int(stmt, i));
                            break;

                        case Type t when t == typeof(string):
                            IntPtr textPtr = SQLiteDLL.sqlite3_column_text(stmt, i);
                            if (textPtr != IntPtr.Zero)
                            {
                                //resultList[i].Add(Marshal.PtrToStringAnsi(textPtr));
                                AddElementToList<string>(resultList[i], Marshal.PtrToStringAnsi(textPtr));
                            }
                            else
                            {
                                resultList[i].Add("NULL");
                            }
                            break;

                        case Type t when t == typeof(double):
                            //resultList[i].Add(SQLiteDLL.sqlite3_column_double(stmt, i));
                            AddElementToList<double>(resultList[i], SQLiteDLL.sqlite3_column_double(stmt, i));
                            break;

                    }
                }
                

                c++;
            }

            SQLiteDLL.sqlite3_finalize(stmt);
            return resultList;
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

        private static (int,IntPtr) Prepare_v2(IntPtr db, string query, out IntPtr stmt)
        {
            IntPtr queryPtr = IntPtr.Zero;
            try
            {
                byte[] queryBytes = System.Text.Encoding.UTF8.GetBytes(query);
                queryPtr = Marshal.AllocHGlobal(queryBytes.Length + 1);
                Marshal.Copy(queryBytes, 0, queryPtr, queryBytes.Length);
                Marshal.WriteByte(queryPtr, queryBytes.Length, 0);

                return (SQLiteDLL.sqlite3_prepare_v2(db, queryPtr, -1, out stmt, IntPtr.Zero),stmt);
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
            Dictionary<string,Type> result = new Dictionary<string,Type>();
            (p, stmt)  = Prepare_v2(_db, query, out stmt);
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

                    case "REAL" :
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

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_text")]
        internal static extern IntPtr sqlite3_column_text(IntPtr stmHandle, int iCol);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_double")]
        internal static extern double sqlite3_column_double(IntPtr stmHandle, int iCol);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_blob")]
        internal static extern IntPtr sqlite3_column_blob(IntPtr stmHandle, int iCol);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_bytes")]
        internal static extern int sqlite3_column_bytes(IntPtr stmHandle, int iCol);

        [DllImport("sqlite3", EntryPoint = "sqlite3_exec")]
        internal static extern int sqlite3_exec(IntPtr db, string sql, IntPtr callback, IntPtr args, out IntPtr errMsg);

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
                            TableValue.Add($"\"{_value}\"");
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
