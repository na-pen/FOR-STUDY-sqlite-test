using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

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
