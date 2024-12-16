using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
using Microsoft.VisualBasic.Logging;

namespace WinFormsApp1
{
    public partial class ClientWindiw : Form
    {
        public int _id;
        public string _name;
        public string _lname;
        private DataTable currentDataTable1 = new DataTable();
        private DataTable currentDataTable2 = new DataTable();
        private readonly string connectionString = "Server=MARCHENKO\\SQLEXPRESS;Initial Catalog=QuickOrders;Trust Server Certificate=True;Integrated Security=True;";
        public ClientWindiw(int id,string name,string lname)
        {
            InitializeComponent();
            _id = id;
            _name = name;
            _lname = lname;
            LoadTableData("Orders", "OrderDetails");
            label1.Text = _id.ToString();
            label7.Text = _name.ToString();
            label8.Text = _lname.ToString();
        }
        private void LoadTableData(string tableName1, string tableName2)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    string queryOrders = $"SELECT OrdersDate,StatusOrder,TotalAmount FROM {tableName1} WHERE ClientID = @ClientID";
                    var ordersData = connection.Query(queryOrders, new { ClientID = _id }).ToList();

                    if (ordersData.Any())
                    {
                        foreach (var column in ((IDictionary<string, object>)ordersData.First()).Keys)
                        {
                            currentDataTable1.Columns.Add(column);
                        }

                        foreach (var row in ordersData)
                        {
                            var values = ((IDictionary<string, object>)row).Values.ToArray();
                            currentDataTable1.Rows.Add(values);
                        }

                        dataGridView1.DataSource = currentDataTable1;
                    }
                    else
                    {
                        MessageBox.Show("У вас нет заказов.");
                    }

                    string queryDetails = $@"
                    SELECT ProductName, Quantity, UnitPrice,TotalPrice
                    FROM {tableName2} 
                    WHERE OrderId IN (
                        SELECT OrderId FROM {tableName1} WHERE ClientID = @ClientID
                    )";

                    var detailsData = connection.Query(queryDetails, new { ClientID = _id }).ToList();

                    if (detailsData.Any())
                    {
                        foreach (var column in ((IDictionary<string, object>)detailsData.First()).Keys)
                        {
                            currentDataTable2.Columns.Add(column);
                        }

                        foreach (var row in detailsData)
                        {
                            var values = ((IDictionary<string, object>)row).Values.ToArray();
                            currentDataTable2.Rows.Add(values);
                        }

                        dataGridView2.DataSource = currentDataTable2;
                    }
                    else
                    {
                        MessageBox.Show("У вас нет деталей заказа.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                var selectedRow = dataGridView1.Rows[e.RowIndex];
                var orderDate = selectedRow.Cells["OrdersDate"].Value.ToString();
                var orderStatus = selectedRow.Cells["StatusOrder"].Value.ToString();
                var totalAmount = Convert.ToDecimal(selectedRow.Cells["TotalAmount"].Value);

                using (var connection = new SqlConnection(connectionString))
                {
                    string queryOrderId = @"
                    SELECT OrderId 
                    FROM Orders 
                    WHERE ClientID = @ClientID 
                    AND OrdersDate = @OrdersDate 
                    AND TotalAmount = @TotalAmount";

                    var orderId = connection.QueryFirstOrDefault<int>(queryOrderId, new
                    {
                        ClientID = _id,
                        OrdersDate = orderDate,
                        TotalAmount = totalAmount
                    });

                    if (orderId == 0)
                    {
                        MessageBox.Show("Не удалось найти OrderId для выбранного заказа.");
                        return;
                    }

                    string queryDetails = @"
                    SELECT ProductName, Quantity, UnitPrice, TotalPrice
                    FROM OrderDetails
                    WHERE OrderId = @OrderId";

                    var orderDetails = connection.Query(queryDetails, new { OrderId = orderId }).ToList();

                    int totalQuantity = 0;
                    decimal totalDetailsCost = 0;

                    foreach (var detail in orderDetails)
                    {
                        totalQuantity += Convert.ToInt32(detail.Quantity);
                        totalDetailsCost += Convert.ToDecimal(detail.TotalPrice);
                    }
                    decimal finalOrderCost = totalAmount + totalDetailsCost;

                    label11.Text = orderStatus;                   
                    label13.Text = orderDate;                     
                    label15.Text = totalAmount.ToString();     
                    label17.Text = totalQuantity.ToString();       
                    label19.Text = totalDetailsCost.ToString();
                    label20.Text = finalOrderCost.ToString(); 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обработке данных: {ex.Message}");
            }
        }
    }
}
