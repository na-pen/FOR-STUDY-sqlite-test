using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SQLite : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Type t = typeof(SQLite.TestClass);

        Console console = new Console();


        //メソッドの一覧を取得する
        MethodInfo[] methods = t.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static);

        //console.Write2Console(t.FullName);
        foreach (MethodInfo m in methods)
        {
            /*
            //特別な名前のメソッドは表示しない
            if (m.IsSpecialName)
                continue;
            */
            string name = m.Name;
            //メソッド名を表示
            console.Write2Console(name);
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public class TestClass
    {
        //列挙型
        public int ID { get; set; }
        public string Name { get; set; }

    }

}
