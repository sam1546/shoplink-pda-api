using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Repository.Interface;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Xml;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Data;
using System.Configuration;
using Newtonsoft.Json;
using System.Threading;
using System.Text.RegularExpressions;

using Excel123 = Microsoft.Office.Interop.Excel;
using System.Reflection;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CoreAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/Dashboard/")]
    public class DashboardController : Controller
    {
        public IOperation operationRepo { get; set; }
        public IConfiguration Configuration;
        WebRequest req = null;
        WebResponse rsp = null;
        XmlDocument xml1 = null;
        XmlNodeList nodes = null;
        string uri = "";
        CredentialCache cc = null;
        NetworkCredential credentials = null;
        string UserName = "";
        string Password = "";
        public static string loginuser = "";
        //PD1_SMARTCAM Welcome1234
        StreamWriter writer = null;
        StreamReader sr = null;
        string result = "";

        private readonly IHostingEnvironment _env;

        string Request, Response, ShopAck, WCAssign, WCAssignini, GWCAssign, PaintWCAssign, WeldWCAssign = "";
        string ShoplinkMirUrl, ShoplinkAck, All, NB, NM, BM, Notch, HM, Bend = "";
        string exc = "";

        DataSet ds = null;
        public static string[] machineGroup = { "Group1" };
        public TimeSpan shift1start = new TimeSpan(0, 0, 0, 0, 0);
        public TimeSpan shift2start = new TimeSpan(0, 0, 0, 0, 0);
        public TimeSpan shift3start = new TimeSpan(0, 0, 0, 0, 0);

        //MachinePlan login1 = new MachinePlan();
        string UserRole = "";
        SqlConnection Conn;
        SqlCommand com = new SqlCommand();
        public DashboardController(IOperation _dashboardRepo, IConfiguration _configuration, IHostingEnvironment env)
        {
            operationRepo = _dashboardRepo;
            Configuration = _configuration;
            _env = env;

            Request = _env.ContentRootPath + "//files//xml//Request.xml";
            Response = _env.ContentRootPath + "//files//xml//Response.xml";
            ShopAck = _env.ContentRootPath + "//files//xml//Shoplink_Acknowledgement.xml";
            WCAssign = _env.ContentRootPath + "//files//xml//Shoplink_WorkCenterAssignment.xml";
            GWCAssign = _env.ContentRootPath + "//files//xml//Galvanization_WorkCenterAssignment.xml";
            PaintWCAssign = _env.ContentRootPath + "//files//xml//PAINT_WorkCenterAssignment.xml";
            WeldWCAssign = _env.ContentRootPath + "//files//Weld_WorkCenterAssignment.xml";
            ShoplinkMirUrl = _env.ContentRootPath + "//files//ini//ShoplinkMirUrl.ini";
            ShoplinkAck = _env.ContentRootPath + "//files//ini//ShoplinkAck.ini";
            All = _env.ContentRootPath + "//files//xml//All.ini";
            WCAssignini = _env.ContentRootPath + "//files//ini//WCAssign.ini";
            NB = _env.ContentRootPath + "//files//xml//NB.xml";
            NM = _env.ContentRootPath + "//files//xml//NM.xml";
            BM = _env.ContentRootPath + "//files//xml//BM.xml";
            Notch = _env.ContentRootPath + "//files//xml//Notch.xml";
            HM = _env.ContentRootPath + "//files//xml//HM.xml";
            Bend = _env.ContentRootPath + "//files//xml//Bend.xml";

            exc = _env.ContentRootPath + "//files//xlsx//Book1.xlsx";


            Conn = new SqlConnection(Configuration.GetConnectionString("DefaultConnection").ToString());
        }

        public string GetTextFromXMLFile(string file)
        {
            StreamReader reader = new StreamReader(file);
            string ret = reader.ReadToEnd();
            reader.Close();
            return ret;
        }

        [HttpGet]
        [Route("getBpByMirno")]
        public async Task<IActionResult> getBpByMirno(string mirno)
        {
            Conn.Open();
            SqlCommand cmd2 = new SqlCommand("select *  from Operations where Mirno='" + mirno + "' ", Conn);
            SqlDataReader dr1 = cmd2.ExecuteReader();
            if (dr1.Read())
            {
                var res = Ok(dr1["BP"].ToString().Trim());
                dr1.Close();
                Conn.Close();
                return res;
            }
            else
            {
                dr1.Close();
                Conn.Close();
                return Ok("");
            }
        }

       
        [HttpGet]
        [Route("loadWorkCenters")]
        public async Task<IActionResult> LoadWorkCenters(string group)
        {
            var res = await operationRepo.GetWorkCeters(group);
            return Ok(res);
        } 

        [HttpGet]
        [Route("bindDataGrid")]
        public async Task<IActionResult> BindDataGrid(string mirno, string plantCode)
        {
            string CommandText = "SELECT RSNo as RSNo,JDDate,FGItem as ElementNo,Mirno as MIRNO,Pices as QTY,Wheight,TotalWt,Operation,Tot_OPS,Length,SctDinemtion as SECTION,LotCode as Billable_Lot, Status FROM Operations where Mirno='" + mirno + "' and BP='" + plantCode + "' and Flag_Fab is null order by [index] desc ";
            SqlCommand myCommand = new SqlCommand(CommandText, Conn);
            SqlDataAdapter myAdapter = new SqlDataAdapter();
            myAdapter.SelectCommand = myCommand;
            DataSet myDataSet = new DataSet();
            Conn.Open();
            myAdapter.Fill(myDataSet);
            Conn.Close();
            //string json = JsonConvert.SerializeObject(myDataSet.Tables[0], Newtonsoft.Json.Formatting.Indented);
            return Ok(myDataSet.Tables[0]);
        } 

        [HttpGet]
        [Route("gettotalWO_Totalreleased")]
        public async Task<IActionResult> GettotalWO_Totalreleased(string mirno)
        {
            Boolean flag = false;
            SqlCommand check;
            int totalWO = 0, Totalreleased = 0;
            check = new SqlCommand("select count(RSNo) as TotalWo from Operations where Mirno='" + mirno + "'   ", Conn); //and POType='Primary'
            SqlDataReader drcount = check.ExecuteReader();
            if (drcount.Read())
            {
                totalWO = int.Parse(drcount["TotalWo"].ToString());
            }
            drcount.Close();

            check = new SqlCommand("select count(RSNo) as TotalWo from Operations where Mirno='" + mirno + "' and Flag_Ack='TRUE' ", Conn); //and POType='Primary' 
            SqlDataReader drack = check.ExecuteReader();
            if (drack.Read())
            {
                Totalreleased = int.Parse(drack["TotalWo"].ToString());
            }
            drack.Close();
            if (totalWO != Totalreleased)
            {
                flag = true;
            }
            return Ok(flag);

        }

        public class jsonResult {
            public int releasedPO { get; set; }
            public int totalPO { get; set; }
            public int Totalreleased { get; set; }
        }

        [HttpGet]
        [Route("releasePO")]
        public async Task<IActionResult> ReleasePO(string mirno, string plantCode)
        {
            jsonResult jsonResult = new jsonResult();
            int releasedPO = 0;
            try
            {
                int totalPO = 0, Totalreleased = 0;
                DateTime AckDate = DateTime.Now;
                string AckJdDate = AckDate.Month + "/" + AckDate.Day + "/" + AckDate.Year;
                SqlCommand cmd = new SqlCommand();
                if (Conn.State == ConnectionState.Closed)
                    Conn.Open();
                SqlCommand c1 = null;
                c1 = new SqlCommand("select count(RSNo) as RSno from Operations where Mirno='" + mirno + "' and BP='" + plantCode + "'  ", Conn); //and POType='Primary'
                SqlDataReader drmir2 = c1.ExecuteReader();
                if (drmir2.Read())
                {
                    totalPO = int.Parse(drmir2["RSno"].ToString());

                }
                drmir2.Close();

                c1 = new SqlCommand("select count(RSNo) as TotalWo from Operations where Mirno='" + mirno + "' and Flag_Ack='TRUE' ", Conn); //and POType='Primary' 
                SqlDataReader drack = c1.ExecuteReader();
                if (drack.Read())
                {
                    Totalreleased = int.Parse(drack["TotalWo"].ToString());
                }
                drack.Close();

                int totalPending = totalPO - Totalreleased;

                jsonResult.totalPO = totalPO;
                jsonResult.Totalreleased = Totalreleased;
                // lblUpdationBar.Visible = true;
                c1 = new SqlCommand("select RSNo from Operations where Mirno='" + mirno + "' and Flag_Ack is null", Conn);
                SqlDataReader dr = c1.ExecuteReader();
                while (dr.Read())
                {
                    try
                    {
                        string RSNo = dr["RSNo"].ToString();
                        xml1 = new XmlDocument();
                        xml1.Load(ShopAck);
                        nodes = xml1.SelectNodes("//WADOCO");
                        foreach (XmlElement element1 in nodes)
                        {
                            element1.InnerText = RSNo;
                            xml1.Save(ShopAck);
                        }
                        sr = new StreamReader(ShoplinkAck);
                        uri = sr.ReadLine();
                        sr.Close();

                        credentials = new NetworkCredential(UserName, Password);

                        cc = new CredentialCache();

                        cc.Add(new Uri(uri), "Basic", credentials);

                        req = WebRequest.Create(uri);
                        req.Method = "POST";
                        req.ContentType = "text/xml";
                        writer = new StreamWriter(req.GetRequestStream());
                        writer.WriteLine(this.GetTextFromXMLFile(ShopAck));


                        writer.Close();
                        req.Credentials = cc;
                        rsp = req.GetResponse();
                        sr = new StreamReader(rsp.GetResponseStream());
                        result = sr.ReadToEnd();
                        sr.Close();
                        Thread.Sleep(100);

                        cmd = new SqlCommand("update Operations set JDDate='" + AckJdDate + "', Flag_Ack='TRUE' where RSNo='" + RSNo + "'", Conn);
                        cmd.ExecuteNonQuery();

                        releasedPO += 1;
                    }
                    catch (Exception ex)
                    {
                        jsonResult.totalPO = 0;
                        jsonResult.Totalreleased = 0;
                        jsonResult.releasedPO = releasedPO;
                    }
                }
                jsonResult.releasedPO = releasedPO;
                dr.Close();
            }
            catch
            {
                jsonResult.totalPO = 0;
                jsonResult.Totalreleased = 0;
                jsonResult.releasedPO = releasedPO;
            }
            return Ok(jsonResult);
        }

        
        [HttpGet]
        [Route("insertUpdateDelete")]
        public async Task<IActionResult> InsertUpdateDelete(string query)
        {
            try
            {
                if (Conn.State == ConnectionState.Closed)
                    Conn.Open();
                SqlCommand com = new SqlCommand(query, Conn);
                int numberOfRecords = com.ExecuteNonQuery();
                if (numberOfRecords > 0)
                    return Ok("true");
                else
                    return Ok("false");
            }
            catch (SqlException ex)
            {
                //Debug.Write(ex.ToString());
                return Ok("false");
            }
            finally
            {
                Conn.Close();
            }
        }

 
        [HttpGet]
        [Route("totweight1")]
        public async Task<IActionResult> Totweight1(string mirno)
        {
            if (Conn.State == ConnectionState.Closed)
                Conn.Open();

            DateTime scandate = DateTime.Parse(DateTime.Now.ToString());
            string scan_Date = scandate.Month + "/" + scandate.Day + "/" + scandate.Year + " " + scandate.TimeOfDay;

            string CommandText = "select sum(TotalWt)/1000 as TotalWheight,count(RSNo) as RSno,sum(Tot_OPS) as TotalOpns,sum(RunTime) as RunTime from Operations where Mirno='" + mirno + "' and MachineName=(select TOP 1 MachineName from Operations where Mirno='" + mirno + "') and PlanningDate=(select TOP 1 PlanningDate from Operations where Mirno='" + mirno + "') and PlanningShift=(select TOP 1 PlanningShift from Operations where Mirno='" + mirno + "')";
            SqlCommand myCommand = new SqlCommand(CommandText, Conn);
            SqlDataAdapter myAdapter = new SqlDataAdapter();
            myAdapter.SelectCommand = myCommand;
            DataSet myDataSet = new DataSet();
            myAdapter.Fill(myDataSet);
            Conn.Close();
            return Ok(myDataSet.Tables[0]);
        }
  
  
    }
}
