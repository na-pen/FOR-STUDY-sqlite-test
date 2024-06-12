using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;

public class SQLite4Cs : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        string pass = Application.dataPath + "/StreamingAssets/" + "existing2.db";
        TestClass result = new TestClass();
        Debug.Log(result.TableName);
        Open(pass);
        Create(new TestClass());

        Insert(
        new[]{
            new TestClass
            {
                ID = 1,
                Name = "test",
            },

            new TestClass
            {
                ID= 2,
                Name = "test2",
            }
        });
        
        Close();
        
         
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void Open(string pass)
    {
        if (sqlite3_open(pass, out _db) != 0)
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
        sqlite3_close(_db);
        Debug.Log("データベースへの接続を終了しました");
        return;
    }
    
    public void Create<T>(T obj) where T : Database
    {
        obj.Parser(obj);
        Debug.Log($"{_db} @Create");


        string query = $"CREATE TABLE {obj.TableName} (";

        for (int i = 0;i < obj.ColumnName.Count; i++)
        {
            query += obj.ColumnName[i] ;

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
            }if (i < obj.ColumnName.Count - 1)
                query += ", ";
        }
        query += ")";
        Debug.Log(query);
        ExecuteQuery(query);

        return;
    }


    public void Insert<T>(T[] obj) where T : Database
    {
        foreach (T a in obj)
        {
            a.Parser(a);
        }

        Debug.Log($"{_db} @Insert");

        string query = $"INSERT INTO {obj[0].TableName}";
        string queryColumns = $"( {String.Join(',', obj[0].ColumnName)} )";
        string queryValue = $"VALUES {string.Join(",", obj.Select(o => $"( {string.Join(',', o.Value)} )"))}";
        query = $"{query} {queryColumns} {queryValue}";
        Debug.Log(query);
        ExecuteQuery(query);

        return;
    }
    /*
    public SQLite4Cs SelectFrom<T>(T resultTable, string[] selectColumn)
    {
        List<string> columnList = this.GetType()
                 .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                 .Select(p => p.Name)
                 .ToArray().ToList();

        DatabaseStructure dbStructure = GetClassProperty(new[] { resultTable });

        bool allElementsInB = ListA.All(item => ListB.Contains(item));

        Debug.Log($"{_db} @SelectFrom");

        string query = $"SELECT {String.Join(",",selectColumn)} FROM {resultTable.GetType().Name}";

        Debug.Log(query);
        ExecuteQuery(query);
        return this;
    }
    */
    private void ExecuteQuery(string query)
    {
        IntPtr errMsg;
        var temp = sqlite3_exec(_db, query, IntPtr.Zero, IntPtr.Zero, out errMsg);
        if (temp != 0)
        {
            string error = Marshal.PtrToStringAnsi(errMsg);
            Debug.LogError("SQLite error: " + error);
        }
    }
    /*
    private void ExecuteQueryStep(string query)
    {
        IntPtr stmt;
        if (sqlite3_prepare_v2(_db, query, query.Length, out stmt, IntPtr.Zero) != 0)
        {
            Debug.LogError("Failed to prepare statement");
            return;
        }

        while (sqlite3_step(stmt) == 100) // SQLITE_ROW = 100
        {
            IntPtr textPtr = sqlite3_column_text(stmt, 1); // Assuming the second column is 'name'
            string name = Marshal.PtrToStringAnsi(textPtr);
            Debug.Log("Name: " + name);
        }

        sqlite3_finalize(stmt);
    }
    */
    public class TestClass : Database
    {
        [PrimaryKey, AutoIncrement]
        public int ID{ get; set; }
        public string Name { get; set; }

    }

    public class Database
    {
        public string TableName = null;
        public List<string> ColumnName = new();
        public List<Type> ColumnType = new();
        public List<Type[]> ColumnAttributes = new();
        public List<object> Value = new();


        internal void Parser<T>(T obj) where T : Database
        {
            Type t = obj.GetType();
            PropertyInfo[] properties = t.GetProperties();

            foreach (var property in properties.Select((value, i) => (value, i)))
            {
                object _value = property.value.GetValue(obj);
                if (_value == null)
                {
                    Value.Add("NULL");
                }
                else
                {

                    switch (property.value.PropertyType)
                    {
                        case Type v when v == typeof(int):
                            Value.Add(_value.ToString());
                            break;

                        case Type v when v == typeof(double):
                            Value.Add(_value.ToString());
                            break;

                        case Type v when v == typeof(float):
                            Value.Add(_value.ToString());
                            break;

                        case Type v when v == typeof(string):
                            Value.Add($"\"{_value}\"");
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


    /* SQLiteのDLL接続関係の設定 */
    [DllImport("sqlite3", EntryPoint = "sqlite3_open")]
    private static extern int sqlite3_open(string filename, out IntPtr db);

    [DllImport("sqlite3", EntryPoint = "sqlite3_close")]
    private static extern int sqlite3_close(IntPtr db);

    [DllImport("sqlite3", EntryPoint = "sqlite3_prepare_v2")]
    private static extern int sqlite3_prepare_v2(IntPtr db, string zSql, int nByte, out IntPtr ppStmpt, IntPtr pzTail);

    [DllImport("sqlite3", EntryPoint = "sqlite3_step")]
    private static extern int sqlite3_step(IntPtr stmHandle);

    [DllImport("sqlite3", EntryPoint = "sqlite3_finalize")]
    private static extern int sqlite3_finalize(IntPtr stmHandle);

    [DllImport("sqlite3", EntryPoint = "sqlite3_errmsg")]
    private static extern IntPtr sqlite3_errmsg(IntPtr db);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_count")]
    private static extern int sqlite3_column_count(IntPtr stmHandle);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_name")]
    private static extern IntPtr sqlite3_column_name(IntPtr stmHandle, int iCol);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_type")]
    private static extern int sqlite3_column_type(IntPtr stmHandle, int iCol);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_int")]
    private static extern int sqlite3_column_int(IntPtr stmHandle, int iCol);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_text")]
    private static extern IntPtr sqlite3_column_text(IntPtr stmHandle, int iCol);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_double")]
    private static extern double sqlite3_column_double(IntPtr stmHandle, int iCol);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_blob")]
    private static extern IntPtr sqlite3_column_blob(IntPtr stmHandle, int iCol);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_bytes")]
    private static extern int sqlite3_column_bytes(IntPtr stmHandle, int iCol);

    [DllImport("sqlite3", EntryPoint = "sqlite3_exec")]
    private static extern int sqlite3_exec(IntPtr db, string sql, IntPtr callback, IntPtr args, out IntPtr errMsg);

    //データベースの接続を表すポインタ
    private IntPtr _db;
    



#if DEBUG
    /// <summary>
    /// 例えば、"TestTable" というテーブル、列名(型) "ID(str),Value(int)" をもつデータベースの場合、このクラスのようにする必要があります 
    /// 
    /// </summary>
    public class TestTable
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
}
