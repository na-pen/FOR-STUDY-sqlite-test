using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using System.Runtime.InteropServices;

public class SQLite : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Test(new[]
        {
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
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void Test<T>(T[] obj) 
    {
        DatabaseStructure databaseStructure = GetClassProperty(obj);
        Debug.Log(databaseStructure);
    }

    public void Insert<T>(T[] obj)
    {
        DatabaseStructure databaseStructure = GetClassProperty(obj);
        Debug.Log(databaseStructure);
    }


    /// <summary>
    /// データベース構造を定義したクラスを要素に持つ配列から、型・変数名・値を取得します
    /// </summary>
    /// <typeparam name="T">データベースの構造 詳しくは<see cref="TestClass">こちら</see></typeparam>
    /// <param name="obj">データベース構造を定義したクラスを要素に持つ配列 
    /// 例えば "TestTable"という構造をもつデータベースの作成や変更を行いたい場合、"TestTable[]"を引数とする必要があります
    /// </param>
    /// <returns><see cref="DatabaseStructure">DatabaseStructure</see> を返します</returns>
    private DatabaseStructure GetClassProperty<T>(T[] obj)
    {
        DatabaseStructure databaseStructure = new DatabaseStructure();

        foreach (var row in obj.Select((value,index) => (value,index)))
        {
            Type t = row.value.GetType();
            PropertyInfo[] properties = t.GetProperties();

            List<object> values = new();
            foreach (var property in properties.Select((value, index) => (value, index)))
            {
                values.Add(property.value.GetValue(row.value));

                object[] attributes = property.value.GetCustomAttributes(true);
                List<string> attributesName = new();
                foreach (object attribute in attributes)
                {
                    attributesName.Add(attribute.GetType().ToString());
                }
                databaseStructure.Columns.Add((new string[] { property.value.Name, property.value.PropertyType.ToString()},attributesName));
            }
            databaseStructure.Value.Add(values);

            databaseStructure.Name = t.Name;
        }

        return databaseStructure;
    }

    public class TestClass
    {
        [AutoIncrement, PrimaryKey]
        public int ID { get; set; }
        public string Name { get; set; }
    }


    /// <summary>データベースの構造を保存するクラス</summary>
    public class DatabaseStructure
    {
        /// <summary>string テーブル名(クラス名)</summary>
        public string Name { get; set; } = null;

        /// <summary>カラム情報&lt;[カラム名,データ型],属性&lt;&gt;&gt;</summary>
        public List<(string[], List<string>)> Columns { get; set; } = new List<(string[], List<string>)>();

        /// <summary>値</summary>
        public List<List<object>> Value { get; set; } = new List<List<object>> ();
    }

    /// <summary>値を自動生成する属性 </summary>
    public class AutoIncrementAttribute : Attribute { }

    /// <summary>カラムをプライマリーキーに設定する属性 </summary>
    public class PrimaryKeyAttribute : Attribute { }



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
    }
    #endif
}
