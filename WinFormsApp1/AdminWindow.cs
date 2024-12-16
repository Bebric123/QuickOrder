using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class AdminWindow : Form
    {
        private DataTable currentDataTable = new DataTable();
        private string selectedTable;
        public string _name;
        private readonly string connectionString = "Server=MARCHENKO\\SQLEXPRESS;Initial Catalog=QuickOrders;Trust Server Certificate=True;Integrated Security=True;";
        public AdminWindow(string name)
        {
            InitializeComponent();
            _name = name;
            label3.Text = _name.ToString(); 
            comboBox1.SelectedItem = "Clients";
            dataGridView1.AllowUserToAddRows = true;
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedTable = comboBox1.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedTable))
            {
                LoadTableData(selectedTable);
            }
        }
        private void LoadTableData(string tableName)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = $"SELECT * FROM {tableName}";
                    var data = connection.Query(query);

                    currentDataTable = new DataTable(); 
                    foreach (var column in ((IDictionary<string, object>)data.First()).Keys)
                    {
                        currentDataTable.Columns.Add(column);
                    }
                    foreach (var row in data)
                    {
                        var values = ((IDictionary<string, object>)row).Values.ToArray();
                        currentDataTable.Rows.Add(values);
                    }

                    dataGridView1.DataSource = currentDataTable;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading table data: {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var newRows = currentDataTable.AsEnumerable().Where(row => row[currentDataTable.Columns[0]] == DBNull.Value || row[currentDataTable.Columns[0]].ToString() == string.Empty).ToList();

                if (newRows.Count == 0)
                {
                    MessageBox.Show("No data for saving");
                    return;
                }

                foreach (var row in newRows)
                {
                    var insertValues = new Dictionary<string, object>();

                    foreach (DataColumn column in currentDataTable.Columns)
                    {
                        if (column.ColumnName == currentDataTable.Columns[0].ColumnName || column.ColumnName == "TotalPrice")
                            continue;

                        object value = row[column] ?? DBNull.Value;

                        if (column.ColumnName == "PasswordHash" && value != DBNull.Value && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            value = HashPassword(value.ToString());
                        }

                        insertValues[column.ColumnName] = value;
                    }
                    string columns = string.Join(", ", insertValues.Keys);
                    string values = string.Join(", ", insertValues.Keys.Select(k => $"@{k}"));
                    string query = $"INSERT INTO {selectedTable} ({columns}) VALUES ({values})";

                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Execute(query, insertValues);
                    }
                }
                MessageBox.Show("New data insert");
                LoadTableData(selectedTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving new rows: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null)
            {
                var row = dataGridView1.CurrentRow;

                if (currentDataTable == null || currentDataTable.Columns.Count == 0)
                {
                    MessageBox.Show("No columns available in the table.");
                    return;
                }

                var idColumn = currentDataTable.Columns[0].ColumnName;

                if (row.Cells[0].Value != null)
                {
                    var id = row.Cells[0].Value;

                    if (MessageBox.Show($"Delete data ID = {id}?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        try
                        {
                            using (var connection = new SqlConnection(connectionString))
                            {
                                string query = $"DELETE FROM {selectedTable} WHERE {idColumn} = @Id";
                                connection.Execute(query, new { Id = id });
                            }
                            currentDataTable.Rows.RemoveAt(row.Index);
                            dataGridView1.DataSource = null;
                            dataGridView1.DataSource = currentDataTable;
                            MessageBox.Show("Data delete");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting row: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("No row selected for deletion.");
            }

        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                var row = dataGridView1.Rows[e.RowIndex];
                var idColumn = currentDataTable.Columns[0].ColumnName;
                var idValue = Convert.ChangeType(row.Cells[0].Value, currentDataTable.Columns[0].DataType);

                if (idValue != null)
                {
                    var updatedValues = new Dictionary<string, object>();

                    for (int i = 0; i < currentDataTable.Columns.Count; i++)
                    {
                        string columnName = currentDataTable.Columns[i].ColumnName;
                        object cellValue = row.Cells[i].Value;

                        if (columnName == "TotalPrice")
                            continue;

                        if (currentDataTable.Columns[i].DataType == typeof(decimal) || currentDataTable.Columns[i].DataType == typeof(float))
                        {
                            if (cellValue == null || string.IsNullOrWhiteSpace(cellValue.ToString()))
                            {
                                cellValue = DBNull.Value; 
                            }
                            else if (!decimal.TryParse(cellValue.ToString(), out decimal parsedValue))
                            {
                                MessageBox.Show($"Eror: '{cellValue}' not for '{columnName}'.");
                                return; 
                            }
                            else
                            {
                                cellValue = parsedValue; 
                            }
                        }
                        else if (cellValue != null && cellValue != DBNull.Value)
                        {
                            cellValue = Convert.ChangeType(cellValue, currentDataTable.Columns[i].DataType);
                        }
                        else
                        {
                            cellValue = DBNull.Value;
                        }
                        updatedValues[columnName] = cellValue;
                    }
                    string setClause = string.Join(", ", updatedValues.Keys.Skip(1).Select(k => $"{k} = @{k}"));
                    string query = $"UPDATE {selectedTable} SET {setClause} WHERE {idColumn} = @Id";

                    using (var connection = new SqlConnection(connectionString))
                    {
                        updatedValues["Id"] = idValue; 
                        connection.Execute(query, updatedValues);
                    }

                    MessageBox.Show("Data save");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error update changes: {ex.Message}");
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
    }
}
