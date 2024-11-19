using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System;
using System.Data;
using System.Diagnostics.Metrics;

namespace EmployeeDBApp
{
    //Класс подключения и работы с БД
    internal class DBConnection
    {
        //Текущее соединение с БД
        private SqlConnection connection;

        public DBConnection(string connectionString)
        {
            connection = new SqlConnection(connectionString);
        }

        //Возвращает состояние текущего соединения
        public ConnectionState ConnectionState()
        {
            return connection.State;
        }

        //Открыть подключение (в случае ошибки возвращаем ее описание)
        async public Task<string> OpenConnection()
        {
            string res = string.Empty;
            try
            {
                await connection.OpenAsync();
            }
            catch (SqlException ex)
            {
                res = "Connection Error: " + ex.Message;
            }
            return res;
        }

        // Закрыть подключение
        async public void CloseConnection()
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        //Добавить нового сотрудника в БД (возвращет результат работы или ошибку)
        async public Task<string> AddNewEmployee(string firstName, string lastName, string email, DateTime dateOfBirth, decimal salary)
        {
            string res = string.Empty;
            string procedureName = "sp_InsertEmployee";
            try
            {
                SqlCommand command = new(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                //Инициализируем параметры хранимой процедуры
                command.Parameters.Add("@FirstName", SqlDbType.VarChar, 50).Value = firstName;
                command.Parameters.Add("@LastName", SqlDbType.VarChar, 50).Value = lastName;
                command.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = email;
                command.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = dateOfBirth;
                command.Parameters.Add("@Salary", SqlDbType.Decimal).Value = salary;

                //Процедура возвращает количество добавленных строк
                var num = await command.ExecuteScalarAsync();
                if (num != null)
                {
                    res = ((int)num == 1) ? "New Employee is add in table" : $"Error: changed {num} rows in the table";
                }
                else
                    res = "There is already Employee in the table with this data";
            }
            catch (SqlException ex)
            {
                res = "Error when adding new Employee in table. " + ex.Message;
            }
            return res;
        }

        //Выбрать всех сотрудников из таблицы (возвращает список строк, если они есть, или список с одной строкой результата запроса)
        async public Task<List<string>> ShowAllEmployees()
        {
            List<string> list = [];
            string procedureName = "sp_GetAllEmployees";

            try
            {
                SqlCommand command = new(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        //Читаем данные и добавляем строку в список
                        while (await reader.ReadAsync())
                        {
                            int id = reader.GetInt32(0);
                            string firstname = reader.GetString(1);
                            string lasttname = reader.GetString(2);
                            string email = reader.GetString(3);
                            DateTime date = reader.GetDateTime(4);
                            decimal salary = reader.GetDecimal(5);
                            list.Add($"{id} \t{firstname} \t{lasttname} \t{email} \t{date.ToString("dd.MM.yyyy")} \t{salary}");
                        }
                    }
                    else
                    {
                        list.Add("No data in table Employees");
                    }
                }
            }
            catch (SqlException ex)
            {
                list.Add("Error when show table Employees. " + ex.Message);
            }
            return list;
        }

        //Обновить информацию по сотруднику (возвращет результат работы или ошибку).
        //Входные параметры именованные со значениями по умолчанию, чтобы можно было подать только один параметр для изменения
        async public Task<string> UpdateEmployeeInfo(string field, int employeeID, string firstName = "", string lastName = "",
                                string email = "", DateTime dateOfBirth = default(DateTime), decimal salary = -1)
        {
            string res = string.Empty;
            // название процедуры
            string procedureName = "sp_UpdateEmployee";
            try
            {
                SqlCommand command = new(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                //Инициализируем параметры хранимой процедуры
                command.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeID;
                command.Parameters.Add("@Field", SqlDbType.VarChar, 15).Value = field;
                command.Parameters.Add("@FirstName", SqlDbType.VarChar, 50).Value = firstName;
                command.Parameters.Add("@LastName", SqlDbType.VarChar, 50).Value = lastName;
                command.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = email;
                command.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = dateOfBirth;
                command.Parameters.Add("@Salary", SqlDbType.Decimal).Value = salary;

                //Процедура возвращает количество измененных строк
                var num = await command.ExecuteScalarAsync();
                if (num != null)
                {
                    res = ((int)num == 1) ? "Employee information is update" : $"Error: changed {num} rows in the table";
                }
                else
                    res = "No changed in the table";
            }
            catch (SqlException ex)
            {
                res = "Error when deleting Employee from table. " + ex.Message;
            }
            return res;
        }

        //Удалить сотрудника (возвращет результат работы или ошибку).
        async public Task<string> DeleteEmployee(int EmployeeID)
        {
            string res = string.Empty;
            // название процедуры
            string procedureName = "sp_DeleteEmployee";
            try
            {
                SqlCommand command = new(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                //Инициализируем параметр хранимой процедуры
                command.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = EmployeeID;

                //Процедура возвращает количество удаленных строк
                var num = await command.ExecuteScalarAsync();
                if (num != null)
                {
                    res = ((int)num == 1) ? "Employee is delete from table" : "Employee with input ID not found";
                }
                else
                    res = "No changed in the table";
            }
            catch (SqlException ex)
            {
                res = "Error when deleting Employee from table. " + ex.Message;
            }
            return res;
        }

        async public void CreateStoredProcedure()
        {
            string sp_Delete = @"CREATE PROCEDURE [dbo].[sp_DeleteEmployee]
                                @EmployeeID int
                            AS
                                SET NOCOUNT ON
                                DELETE from Employees WHERE EmployeeID = @EmployeeID
                                SELECT @@ROWCOUNT
                            ";

            string sp_Select = @"CREATE PROCEDURE [dbo].[sp_GetAllEmployees]
                                AS
                                    SET NOCOUNT ON;
                                    SELECT * from Employees
                                ";

            string sp_Insert = @"CREATE PROCEDURE [dbo].[sp_InsertEmployee]
                                @FirstName nvarchar(50),
                                @LastName nvarchar(50),
	                            @Email nvarchar(100),
                                @DateOfBirth date,
	                            @Salary decimal
                                AS
	                                SET NOCOUNT ON;
	
	                                IF not exists (Select EmployeeID from Employees where FirstName = @FirstName and LastName = @LastName and Email = @Email and DateOfBirth = @DateOfBirth)
	                                BEGIN
	                                    INSERT INTO Employees (FirstName, LastName, Email, DateOfBirth, Salary) VALUES (@FirstName, @LastName, @Email, @DateOfBirth, @Salary)

	                                    SELECT @@ROWCOUNT
                                    END
                                ";

            string sp_Update = @"CREATE PROCEDURE [dbo].[sp_UpdateEmployee]
	                            @EmployeeID int,
	                            @Field nvarchar(15),
	                            @FirstName nvarchar(50),
                                @LastName nvarchar(50),
	                            @Email nvarchar(100),
                                @DateOfBirth date,
	                            @Salary decimal
                            AS
	                            SET NOCOUNT ON;

	                            IF @Field = 'FirstName'
	                            BEGIN
                                    UPDATE Employees SET FirstName = @FirstName WHERE EmployeeID=@EmployeeID
		                            SELECT @@ROWCOUNT
	                            END
                                IF @Field = 'LastName'
	                            BEGIN
                                    UPDATE Employees SET LastName = @LastName WHERE EmployeeID=@EmployeeID
	                                SELECT @@ROWCOUNT
	                            END
	                            IF @Field = 'Email'
	                            BEGIN
                                    UPDATE Employees SET Email = @Email WHERE EmployeeID=@EmployeeID
		                            SELECT @@ROWCOUNT
	                            END
                                IF @Field = 'DateOfBirth'
	                            BEGIN
                                    UPDATE Employees SET DateOfBirth = @DateOfBirth WHERE EmployeeID=@EmployeeID
	                                SELECT @@ROWCOUNT
	                            END
                                IF @Field = 'Salary'
	                            BEGIN
                                    UPDATE Employees SET Salary = @Salary WHERE EmployeeID=@EmployeeID
	                                SELECT @@ROWCOUNT
	                            END
                            ";
            try
            {
                SqlCommand command = new SqlCommand(sp_Delete, connection);
                await command.ExecuteNonQueryAsync();

                command = new SqlCommand(sp_Select, connection);
                await command.ExecuteNonQueryAsync();

                command = new SqlCommand(sp_Insert, connection);
                await command.ExecuteNonQueryAsync();

                command = new SqlCommand(sp_Update, connection);
                await command.ExecuteNonQueryAsync();

                Console.WriteLine("Stored Procedure is adding to Database");
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Error when add stored procedure. " + ex.Message);
            }
        }
    }
}