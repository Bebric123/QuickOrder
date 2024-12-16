using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Dapper;
using Microsoft.Data.SqlClient;

namespace WinFormsApp1
{
    public partial class Register : Form
    {
        private readonly string connectionString = "Server=MARCHENKO\\SQLEXPRESS;Initial Catalog=QuickOrders;Trust Server Certificate=True;Integrated Security=True;";
        public Register()
        {
            InitializeComponent();
        }
        private void button1_Click_1(object sender, EventArgs e)
        {
            string login = richTextBox1.Text;
            string password = HashPassword(textBox1.Text);

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter login/password");
                return;
            }

            using (var connection = new SqlConnection(connectionString))
            {
                string adminQuery = "SELECT PasswordHash,UserName FROM Admins WHERE UserName = @UserName AND PasswordHash = @PasswordHash";
                var adminData = connection.QueryFirstOrDefault<(string PasswordHash, string UserName)>(adminQuery, new { UserName = login, PasswordHash = password });

                if (adminData.PasswordHash != null)
                {
                    AdminWindow adminForm = new AdminWindow(adminData.UserName);
                    adminForm.Show();
                }

                string clientQuery = "SELECT PasswordHash, ClientId,FirstName,LastName FROM Clients WHERE FirstName = @FirstName AND PasswordHash = @PasswordHash";
                var clientData = connection.QueryFirstOrDefault<(string PasswordHash, int ClientId, string FirstName, string LastName)>(clientQuery, new { FirstName = login, PasswordHash = password });

                if (clientData.PasswordHash != null)
                {
                    ClientWindiw clientForm = new ClientWindiw(clientData.ClientId, clientData.FirstName, clientData.LastName);
                    clientForm.Show();
                    return;
                }
            }
        }
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox1.UseSystemPasswordChar)
            {
                textBox1.UseSystemPasswordChar = false;
            }
            else
            {
                textBox1.UseSystemPasswordChar = true;
            }
        }
    }
}

