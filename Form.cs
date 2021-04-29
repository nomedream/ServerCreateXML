using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ServerCreateXML
{
    public partial class Form : System.Windows.Forms.Form
    {
        public Form()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, EventArgs e)
        {
            CreateXML();
        }

        /// <summary>
        /// 生成XML
        /// </summary>
        public void CreateXML()
        {
            // 获取当前文件夹路径，即根路径
            // "...bin\\Debug"
            string currentDirPath = Directory.GetCurrentDirectory();

            XDocument xDocument = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("updateFiles",
                    new XAttribute("mainVersion", ConfigurationManager.AppSettings["mainVersion"])
                )
            );

            // 获取更新文件夹目录
            // "...bin\\Debug\\FileFolder"
            string updateFileDir = Path.Combine(currentDirPath, ConfigurationManager.AppSettings["updateFileDirName"]);

            DirectoryInfo updateFileDirInfo = new DirectoryInfo(updateFileDir);

            if (File.Exists(ConfigurationManager.AppSettings["configName"]))
            {
                File.Copy(ConfigurationManager.AppSettings["configName"], "tempConfig.xml");
            }

            xDocument.Save(ConfigurationManager.AppSettings["configName"]);
            // MessageBox.Show("初始化");

            BuildXML(xDocument, updateFileDirInfo, currentDirPath);
            xDocument.Save(ConfigurationManager.AppSettings["configName"]);
            UpdateAppConfig("mainVersion", xDocument.Root.Attribute("mainVersion").Value);

        }

        public void BuildXML(XDocument xDocument, DirectoryInfo dirInfo, string ftpRootDirPath)
        {
            // 判断文件夹是否存在，不存在则创建
            if (!Directory.Exists(dirInfo.FullName))
            {
                Directory.CreateDirectory(dirInfo.FullName);
            }

            foreach (var file in dirInfo.GetFiles())
            {
                string name = GetPartPathFileName(file.FullName, ftpRootDirPath);
                string hash = GetSHA256(file.FullName);
                string mainVersion = ConfigurationManager.AppSettings["mainVersion"];

                xDocument.Root.Add(new XElement("file",
                        new XAttribute("name", name),
                        // new XAttribute("src", ""),
                        new XAttribute("version", mainVersion),
                        new XAttribute("hash", hash)
                    // new XAttribute("size", 0),
                    // new XAttribute("option", "add")
                    )
                );

                xDocument.Save(Path.Combine(ftpRootDirPath, ConfigurationManager.AppSettings["configName"]));

                var tempDoc = XDocument.Load(Path.Combine(ftpRootDirPath, "tempConfig.xml"));
                var query = from fileElement in tempDoc.Root.Elements("file")
                            where fileElement.Attribute("name").Value == name && fileElement.Attribute("hash").Value != hash
                            select fileElement.Attribute("name").Value;

                foreach (var updateName in query)
                {
                    MessageBox.Show(updateName);
                    int version = Convert.ToInt32(mainVersion) + 1;
                    xDocument.Root.Attribute("mainVersion").Value = version.ToString();
                    // xDocument.Root.Elements("file").Where(v => v.Attribute("name").Value == updateName);
                    var fileElementItems = from fileElement in xDocument.Root.Elements("file")
                                           where fileElement.Attribute("name").Value == updateName
                                           select fileElement;
                    foreach (var item in fileElementItems)
                    {
                        item.Attribute("version").Value = version.ToString();
                        MessageBox.Show("OK");
                    }

                }

            }

            // 递归子文件夹
            foreach (var dir in dirInfo.GetDirectories())
            {
                DirectoryInfo subDirInfo = new DirectoryInfo(dir.FullName);
                BuildXML(xDocument, subDirInfo, ftpRootDirPath);
            }
        }

        public string UpdateVersion()
        {
            string version = null;



            return version;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileFullPath"></param>
        /// <param name="ftpRootDirPath"></param>
        /// <returns></returns>
        public string GetPartPathFileName(string fileFullPath, string ftpRootDirPath)
        {
            string name;

            name = fileFullPath.Replace(ftpRootDirPath + "\\", "").Replace(ConfigurationManager.AppSettings["updateFileDirName"] + "\\", "");

            return name;
        }

        /// <summary>
        /// 获取文件的 SHA256 值
        /// </summary>
        /// <param name="fileFullPath">文件的绝对路径</param>
        /// <returns></returns>
        public string GetSHA256(string fileFullPath)
        {
            string hash = null;
            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(fileFullPath))
                {
                    byte[] bytes = sha256.ComputeHash(fileStream);

                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        builder.Append(bytes[i].ToString("x2"));
                    }
                    hash = builder.ToString().ToUpper();
                }
            }
            return hash;
        }

        /// <summary>
        /// 更新App.config
        /// </summary>
        /// <param name="key">需要更新的节点</param>
        /// <param name="value">指定节点的更新值</param>
        /// <returns></returns>
        /// 
        public static void UpdateAppConfig(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = value;
            configuration.Save(ConfigurationSaveMode.Full);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
