using System.Collections;
using UnityEngine;
using SQLite4Cs;
using System.Linq;
using System.Collections.Generic;
using System;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SQLite sqlite = new();
        string pass = Application.dataPath + "/StreamingAssets/" + "test.db";
        sqlite.Open(pass);
        sqlite.Create<TestClass>();
        
        sqlite.Insert(
        new[]{
            new TestClass
            {
                Name = "テスト",
                Option = 0
            },

            new TestClass
            {
                Name = "テスト１",
                Option = 1
            },
            new TestClass
            {
                Name = "أسد",
                Option = 1
            },
            new TestClass
            {
                Name = "test",
                Option = 2
            },
            new TestClass
            {
                Name = "test2",
                Option = 2
            },
            new TestClass
            {
                Name = "test2",
                Option = 3
            }
        });
        var result = sqlite.Table<TestClass>().Select(new[] { "ID","Name" }).Where(x => x.Name == "test").Do();
        sqlite.Close();

        foreach (var list in result)
        {
            /*foreach (var item in list)
            {
                Debug.Log(item);
            }*/
        }


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class TestClass : Database
{
    [PrimaryKey, AutoIncrement]
    public int ID { get; set; }
    public string Name { get; set; }
    public int Option { get; set; }

}
