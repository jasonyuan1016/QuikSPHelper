using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuikSPHelper
{
    public partial class Form1 : Form
    {
        string connStr = ConfigurationManager.ConnectionStrings["connStr"].ConnectionString;
        public Form1()
        {
            InitializeComponent();

            InitTable();
            for (int i = 0; i < cbsAction.Items.Count; i++)
            {
                cbsAction.SetItemChecked(i, true);
            }
        }

        void InitTable()
        {
            sltTable.Items.Clear();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                //建立连接
                conn.Open();

                //创建SQL命令
                SqlCommand queryCmd = new SqlCommand("SELECT [Name] FROM SysObjects WHERE XType='U' ORDER BY [Name]", conn);


                //执行SQL命令
                SqlDataReader reader = queryCmd.ExecuteReader();
                //处理SQL命令结果
                while (reader.Read())
                {
                    sltTable.Items.Add(reader[0]);
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (sltTable.SelectedItem == null)
            {
                MessageBox.Show("选表！");
                return;
            }
            txtContent.Text = string.Empty;
            string tName = sltTable.SelectedItem.ToString();
            string spName = txtName.Text;
            var columns = GetColumnItems();
            AppendText("-- =============================================");
            AppendText("-- Author:" + txtAuthor.Text);
            AppendText("-- Create date: " + DateTime.Now.ToString("yyyy.MM.dd"));
            AppendText("-- Description:" + txtDescription.Text);
            if (!string.IsNullOrEmpty(txtIDAL.Text))
            {
                AppendText("-- IDAL         " + txtIDAL.Text);
            }
            if (!string.IsNullOrEmpty(txtDAL.Text))
            {
                AppendText("-- DAL          " + txtDAL.Text);
            }
            if (!string.IsNullOrEmpty(txtMethod.Text))
            {
                AppendText("-- Method       " + txtMethod.Text);
            }
            if (!string.IsNullOrEmpty(txtReturn.Text))
            {
                AppendText("-- Return       " + txtReturn.Text);
            }
            AppendText("/*");
            AppendText("EXEC " + spName);
            string EXECParams = string.Empty;
            foreach (var itm in columns)
            {
                if (itm._type.Contains("char"))
                {
                    EXECParams += ("," + c1(itm._name) + "=N''");
                }
                else if (itm._type.Contains("date"))
                {
                    EXECParams += ("," + c1(itm._name) + "='" + DateTime.Now.ToString("yyyy-MM-dd") + "'");
                }
                else
                {
                    EXECParams += ("," + c1(itm._name) + "=0");
                }
            }
            AppendText(EXECParams.Substring(1));
            AppendText("*/");
            AppendText("-- =============================================");
            AppendText("CREATE PROCEDURE [dbo].["+ spName + "]");
            AppendText(ConvertParas(columns[0]), 1);
            for (int i = 1; i < columns.Count; i++)
            {
                AppendText("," + ConvertParas(columns[i]),1);
            }
            AppendText("AS");
            AppendText("BEGIN");
            AppendText("SET NOCOUNT ON;", 1);
            string actStr = string.Empty;
            string tmpStr = string.Empty;
            for (int i = 0; i < cbsAction.CheckedItems.Count; i++)
            {
                switch (cbsAction.CheckedItems[i].ToString())
                {
                    case "SELECT":
                        AppendText("SELECT ", 2);
                        AppendText(columns[0]._name + " " + c(columns[0]._name), 3);
                        for (int x = 1; x < columns.Count; x++)
                        {
                            AppendText("," + columns[x]._name + " " + c(columns[x]._name), 3);
                        }
                        AppendText(" FROM " + tName, 2);
                        AppendText(" WHERE 1=1 ", 2);
                        AppendText("");
                        break;
                    case "ADD":
                        AppendText("INSERT INTO " + tName, 2);
                        tmpStr = "(" + columns[0]._name;
                        for (int x = 1; x < columns.Count; x++)
                        {
                            tmpStr += "," + columns[x]._name;
                        }
                        tmpStr += ")";
                        AppendText(tmpStr, 2);
                        AppendText(" VALUES",2);
                        tmpStr = "(" + c1(columns[0]._name);
                        for (int x = 1; x < columns.Count; x++)
                        {
                            tmpStr += "," + c1(columns[x]._name);
                        }
                        tmpStr += ")";
                        AppendText(tmpStr, 2);
                        AppendText(" SELECT 0", 2);
                        AppendText("");
                        break;
                    case "UPDATE":
                        AppendText("UPDATE " + tName, 2);
                        AppendText(" SET " + columns[0]._name + "=" + c1(columns[0]._name), 3);
                        for (int x = 1; x < columns.Count; x++)
                        {
                            AppendText("," + columns[x]._name + "=" + c1(columns[x]._name), 3);
                        }
                        AppendText(" WHERE 1=1", 2);
                        AppendText(" SELECT 0", 2);
                        AppendText("");
                        break;
                }
            }
            AppendText("END");
        }

        private List<ColumnItem> GetColumnItems()
        {
            string sql = "SELECT * FROM V_GetTabDefine WHERE tName='" + sltTable.SelectedItem.ToString() + "'";
            List<ColumnItem> columns = new List<ColumnItem>();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                //建立连接
                conn.Open();

                //创建SQL命令
                SqlCommand queryCmd = new SqlCommand(sql, conn);


                //执行SQL命令
                SqlDataReader reader = queryCmd.ExecuteReader();
                //处理SQL命令结果
                while (reader.Read())
                {
                    columns.Add(new ColumnItem()
                    {
                        _name = reader["_name"].ToString(),
                        _description = reader["_description"].ToString(),
                        _pk = reader["_pk"].ToString().Equals("PK"),
                        _type = reader["_type"].ToString(),
                        _length = Convert.ToInt32(reader["_length"]),
                        _places = Convert.ToInt32(reader["_places"])
                    });
                }
            }
            return columns;
        }

        private string ConvertParas(ColumnItem item)
        {
            string str = c1(item._name) + " as " + item._type;
            switch (item._type)
            {
                case "int":
                case "tinyint":
                case "datetime":
                case "bigint":
                case "bit":
                    break;
                default:
                    if (item._places > 0)
                    {
                        str += "(" + item._length + "," + item._places + ")";
                    }
                    else
                    {
                        str += "(" + item._length + ")";
                    }
                    break;
            }
            
            if (item._description != string.Empty)
            {
                str += "    " + "--" + item._description;
            }
            return str;
        }

        /// <summary>
        /// 去前缀
        /// </summary>
        /// <param name="p">表字段</param>
        /// <returns></returns>
        private string c(string p)
        {
            return p.Substring(p.IndexOf('_'));
        }

        /// <summary>
        /// 去前缀，加@前缀
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private string c1(string p)
        {
            return "@" + c(p);
        }

        /// <summary>
        /// 追加内容到文本框
        /// </summary>
        /// <param name="str">内容</param>
        /// <param name="indentCount">缩进次数</param>
        private void AppendText(string str, int indentCount = 0)
        {
            for (int i = 0; i < indentCount; i++)
            {
                txtContent.AppendText("    ");
            }
            txtContent.AppendText(str);
            txtContent.AppendText(System.Environment.NewLine);
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (txtContent.Text.Length > 0)
            {
                Clipboard.SetDataObject(txtContent.Text);
                MessageBox.Show("已将内容复制到剪贴板");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string funName = txtMethod.Text;
            List<ColumnItem> columns = null;
            try
            {
                columns = GetColumnItemsFromSP();
            }
            catch (Exception)
            {
                MessageBox.Show("读取SP异常");
            }
            txtContent.Text = string.Empty;
            AppendText("/// <summary>", 2);
            AppendText("/// " + txtDescription.Text, 2);
            AppendText("/// </summary>", 2);
            AppendText("/// <returns></returns>",2);
            AppendText("private string " + funName.ToLower() + "()", 2);
            AppendText("{",2);
            AppendText("ResultBase resultObj = new ResultBase();", 3);
            AppendText("");
            AppendText("try",3);
            AppendText("{", 3);
            AppendText("Dictionary<string, ParaItem> paras = new Dictionary<string, ParaItem>();", 4);
            foreach (var item in columns)
            {
                WriteParaItem(item);
            }
          
            AppendText("");
            AppendText("#region 验证数据格式是否有效", 4);

            AppendText("Task<string>[] tooltips = new Task<string>[]{", 4);
            AppendText("Task.Factory.StartNew(()=>WebUtility.ValidationData(paras[\"" + c(columns[0]._name) + "\"].Value.ToString(), " + ConvertValidateType(columns[0]._type) + ", " + columns[0]._length + ", \"" + c(columns[0]._name) + "\", false)),", 5);
            for (int i = 1; i < columns.Count; i++)
            {
                WriteValidationData(columns[i]);
            }
            AppendText("};", 4);

            AppendText("string tips = WebUtility.ComValidation(tooltips);", 4);
            AppendText("if (tips.Length > 0)", 4);
            AppendText("{", 4);
            AppendText("//验证不正确返回异常的Json", 5);
            AppendText("resultObj.errorInfo = WebUtility.SetTipsInfo(tips);", 5);
            AppendText("resultObj.result = false;", 5);
            AppendText("return resultObj.ToString();", 5);
            AppendText("}", 4);

            AppendText("#endregion", 4);
            AppendText("");

            if (txtReturn.Text.Equals("int", StringComparison.CurrentCultureIgnoreCase))
            {
                AppendText("resultObj.data = DALUtility." + txtDAL.Text + "." + funName + "(paras);", 4);
            }
            else
            {
                AppendText("DataSet ds = DALUtility." + txtDAL.Text + "." + funName + "(paras);", 4);
                AppendText("resultObj.data = new ResultData()", 4);
                AppendText("{", 4);
                AppendText("rows = ds", 5);
                AppendText("};", 4);
            }
            AppendText("}", 3);
            AppendText("catch (Exception ex)", 3);
            AppendText("{", 3);
            AppendText("resultObj.result = false;", 4);
            AppendText("resultObj.errorInfo = WebUtility.SetErrorInfo(ex.ToString());", 4);
            AppendText("DALUtility.Log.SaveExceptionLog(resultObj.errorInfo.errorCode, ex.ToString());", 4);
            AppendText("}", 3);
            AppendText("");
            AppendText("return resultObj.ToString();", 4);
            AppendText("}", 2);
        }

        void WriteParaItem(ColumnItem item)
        {
            string pName = c(item._name);
            if (pName.Equals("_creator") || pName.Equals("_editor"))
            {
                AppendText("paras[\"" + pName + "\"] = SetProperty(UserName, SqlDbType.VarChar, 30, 2);", 4);
            }
            else
            {
                string[] needLengthTypes = { "decimal", "nvarchar", "varchar" };

                string str = "paras[\"" + pName+ "\"] = SetProperty(\"" + pName+ "\", " + ConvertSqlDbType(item._type);
                if (needLengthTypes.Contains(item._type))
                {
                    str += "," + item._length;
                }
                if (item._output)
                {
                    str += ", ParameterDirection.Output";
                }
                str += ");";
                AppendText(str, 4);
            }
        }

        void WriteValidationData(ColumnItem item)
        {
            string str = "Task.Factory.StartNew(()=>WebUtility.ValidationData(paras[\"" + c(item._name) + "\"].Value.ToString(), " + ConvertValidateType(item._type) + ", " + item._length + ", \"" + c(item._name) + "\", false)),";
            AppendText(str, 5);
        }

        string ConvertSqlDbType(string _type)
        {
            switch (_type)
            {
                case "bigint": return "SqlDbType.BigInt";
                case "bit": return "SqlDbType.Bit";
                case "char": return "SqlDbType.Char";
                case "datetime": return "SqlDbType.DateTime";
                case "decimal": return "SqlDbType.Decimal";
                case "int": return "SqlDbType.Int";
                case "nvarchar": return "SqlDbType.NVarChar";
                case "smallint": return "SqlDbType.SmallInt";
                case "tinyint": return "SqlDbType.TinyInt";
                case "varbinary": return "SqlDbType.VarBinary";
                case "varchar": return "SqlDbType.VarChar";
                default:
                    return "SqlDbType." + _type;
            }
        }

        string ConvertValidateType(string _type)
        {
            switch (_type)
            {
                case "bigint": return "ValidateType.Integer";
                case "bit": return "ValidateType.Boolean";
                case "datetime": return "ValidateType.DateTime";
                case "decimal": return "ValidateType.Number";
                case "int": return "ValidateType.Integer";
                case "smallint": return "ValidateType.Integer";
                case "tinyint": return "ValidateType.Integer";
                default:
                    return "ValidateType.None";
            }
        }

        public List<ColumnItem> GetColumnItemsFromSP()
        {
            List<ColumnItem> columns = new List<ColumnItem>();

            string sql = string.Format("select P.name,parameter_id,P.max_length,is_output,S.name tname from (select name,parameter_id,max_length,is_output,system_type_id from sys.all_parameters where object_id=OBJECT_ID('{0}')) P	left join sys.types S on P.system_type_id = S.system_type_id", txtName.Text);

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                //建立连接
                conn.Open();

                //创建SQL命令
                SqlCommand queryCmd = new SqlCommand(sql, conn);


                //执行SQL命令
                SqlDataReader reader = queryCmd.ExecuteReader();
                //处理SQL命令结果
                while (reader.Read())
                {
                    columns.Add(new ColumnItem()
                    {
                        _name = reader["name"].ToString().Replace("@", ""),
                        _type = reader["tname"].ToString(),
                        _length = Convert.ToInt32(reader["max_length"]),
                        _output = Convert.ToBoolean(reader["is_output"])
                    });
                }
            }

            return columns;

        }
    }

    
    



    /// <summary>
    /// 表字段
    /// </summary>
    public class ColumnItem
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string _name { get; set; }
        /// <summary>
        /// 说明
        /// </summary>
        public string _description { get; set; }
        /// <summary>
        /// 是否为主键
        /// </summary>
        public bool _pk { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public string _type { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        public int _length { get; set; }
        /// <summary>
        /// 小数位
        /// </summary>
        public int _places { get; set; }
        /// <summary>
        /// 是否输出[SP参数专用]
        /// </summary>
        public bool _output { get; set; }
    }
}
