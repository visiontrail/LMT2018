﻿using System;
using SCMT_json.JSONDataMgr;

namespace SCMT_json
{
    class NodeB {
        Database test;
        void ResultInitData(bool result)
        {
            if (result)
            {
                Console.WriteLine("init data result is ok");

                // 查询数据
                IReDataByEnglishName nameInfo = new ReDataByEnglishName();
                test.getDataByEnglishName("srsResourceSetId", out nameInfo);
                Console.WriteLine("output, {0}", nameInfo.oid);
            }
            else
                Console.WriteLine("init data result is failed");
        }

        public void dosomething()
        {
            test = new Database();


            if (!test.initDatabase())
            {
                Console.WriteLine("note b initDatabase faild. ");
            }
            else {
                Console.WriteLine("test.initDatabase ok. ");
            }
            test.resultInitData = new ResultInitData(ResultInitData);

        }
    }

    class Test
    {
        static void Main(String[] args)
        {
            //
            //testForCmdJson();

            //
            testForInitDb();
        }

        static void testForCmdJson()
        {
            //解析.mdb文件
            JsonDataManager JsonDataM = new JsonDataManager("5.10.11");
            JsonDataM = new JsonDataManager("5.10.11");
            JsonDataM.ConvertAccessDbToJsonCmdTree();
            Console.WriteLine("write json ok. ====, time is " + DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒"));
            return;
        }

        static void testForInitDb()
        {
            Console.WriteLine("begin ====, time is " + DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒"));
            NodeB b = new NodeB();
            b.dosomething();
            Console.WriteLine("end ====, time is " + DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒"));

            Console.Read();
        }
        

        private static void CloneMe(ICloneable c, string myStr)
        {
            object theClone = c.Clone();
            Console.WriteLine("Your clone is a:{0}",theClone.GetType().Name);
        }

        static void test()
        {
            string myStr = "hello";
            //CloneMe(myStr);
        }

        public void testCmdTree()
        {
            // 2. 解析lm.dtz => json文件(增加，叶子节点的读写属性)
            //解析.mdb文件
            JsonDataManager JsonDataM = new JsonDataManager("5.10.11");
            JsonDataM = new JsonDataManager("5.10.11");
            JsonDataM.ConvertAccessDbToJsonCmdTree();
            Console.WriteLine("write json ok. ====, time is " + DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒"));
        }

    }
}
