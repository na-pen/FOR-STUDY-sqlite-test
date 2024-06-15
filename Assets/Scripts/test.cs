using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SQLite4Cs;
using System.Linq;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SQLite sqlite = new();
        string pass = Application.dataPath + "/StreamingAssets/" + "test.db";
        TestClass result = new TestClass();
        Debug.Log(result.TableName);
        sqlite.Open(pass);
        sqlite.Create<TestClass>();

        sqlite.Insert(
        new[]{
            new TestClass
            {
                ID = 1,
                Name = "test",
                Option = 0
            },

            new TestClass
            {
                ID= 2,
                Name = "test",
                Option = 1
            },
            new TestClass
            {
                ID= 3,
                Name = "test",
                Option = 1
            },
            new TestClass
            {
                ID= 4,
                Name = "test",
                Option = 2
            },
            new TestClass
            {
                ID= 5,
                Name = "test2",
                Option = 2
            },
            new TestClass
            {
                ID= 6,
                Name = "test2",
                Option = 3
            }
        });

        List<int> ids = new List<int> {1,2,4 };
        Debug.Log(sqlite.Table<TestClass>().SelectFrom(new[] { "ID", "Name" }).Where(x => ids.Contains(x.ID)).Do());
        Debug.Log(sqlite.Table<TestClass>().SelectFrom(new[] { "ID", "Name" }).Where(x => x.Name == "test").And(x => ids.Contains(x.ID)).Or(x => x.Option == 1).Do());

        sqlite.Close();


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
