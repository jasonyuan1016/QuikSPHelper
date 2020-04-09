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
            string sql = "SELECT * FROM V_GetTabDefine WHERE tName='" + tName + "'";
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

            AppendText("-- =============================================");
            AppendText("-- Author:" + txtAuthor.Text);
            AppendText("-- Create date: " + DateTime.Now.ToString("yyyy.MM.dd"));
            AppendText("-- Description:" + txtDescription.Text);
            if (!string.IsNullOrEmpty(txtIDAL.Text))
            {
                AppendText("-- IDAL " + txtIDAL.Text);
            }
            if (!string.IsNullOrEmpty(txtDAL.Text))
            {
                AppendText("-- DAL " + txtDAL.Text);
            }
            if (!string.IsNullOrEmpty(txtMethod.Text))
            {
                AppendText("-- Method " + txtMethod.Text);
            }
            if (!string.IsNullOrEmpty(txtReturn.Text))
            {
                AppendText("-- Return " + txtReturn.Text);
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
            str += "    " + "-" + item._description;
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
    }
}
