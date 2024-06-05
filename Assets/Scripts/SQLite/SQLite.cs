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


        //���\�b�h�̈ꗗ���擾����
        MethodInfo[] methods = t.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static);

        //console.Write2Console(t.FullName);
        foreach (MethodInfo m in methods)
        {
            /*
            //���ʂȖ��O�̃��\�b�h�͕\�����Ȃ�
            if (m.IsSpecialName)
                continue;
            */
            string name = m.Name;
            //���\�b�h����\��
            console.Write2Console(name);
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public class TestClass
    {
        //�񋓌^
        public int ID { get; set; }
        public string Name { get; set; }

    }

}
