using KursovayaDB.Models;
using KursovayaDB.Services.LogServices;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace KursovayaDB.DataBaseServices;

public static class SQLScripts
{
    static string connectionString = @$"Server=DESKTOP-SJLIHQ6;Trusted_Connection=True;Encrypt=False;";
    static string sqlScript = "";

    #region Создание
    public static async Task CreateDatabaseAsync()//Создание Базы данных(OP)
    {
        sqlScript = File.ReadAllText(@"SQL Scripts\CreateDB.sql");

        await ExecuteSQLScriptNonQueryAsync();
        await CreateTablesAsync();
    }
    static async Task CreateTablesAsync()//Создание таблиц БД(OP)
    {
        sqlScript = File.ReadAllText(@"SQL Scripts\CreateTables.sql");
        await ExecuteSQLScriptNonQueryAsync();
        await CreateStoredProcedures();
    }
    static async Task CreateStoredProcedures()//Создание хранимых процедур(OP)
    {
        string[] storedProcedures = new string[]
        {
            File.ReadAllText(@"SQL Scripts\SingUp.sql"),
            File.ReadAllText(@"SQL Scripts\CheckUserProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\AddCategoryProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\AddProductProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\AddProductPriceProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\AddAttributeProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\AddAttributeValueProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\GetAllCategoriesProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\CalculateAndInsertAveragePriceProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\GetAllAveragePricesProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\GetUserPasswordProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\GetAllProductsProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\GetAllPricesProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\GetAllAttributtesProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\GetAllAttributeValuesProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\CalculateAndInsertPriceIndexProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\GetAllPriceIndexesProcedure.sql"),
            File.ReadAllText(@"SQL Scripts\AveragePricesView.sql"),
            File.ReadAllText(@"SQL Scripts\PriceIndexesView.sql"),
            File.ReadAllText(@"SQL Scripts\AttributeValuesView.sql"),
            File.ReadAllText(@"SQL Scripts\GetFirstPriceDate.sql"),
        };

        foreach (string storedProcedure in storedProcedures)
        {
            sqlScript = storedProcedure;
            await ExecuteSQLScriptNonQueryAsync();
        }
    }
    #endregion Создание

    #region Добавление данных
    public static async Task<int> AddUserAsync(string login, string password)//Регистрация пользователя(OP)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                using (SqlCommand command = new SqlCommand("AddUserProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@Login", login);
                    command.Parameters.AddWithValue("@Password", password);

                    SqlParameter resultParameter = new SqlParameter("@Result", SqlDbType.Int);
                    resultParameter.Direction = ParameterDirection.Output;
                    command.Parameters.Add(resultParameter);
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                    await connection.CloseAsync();

                    return (int)resultParameter.Value;
                }
            }
        }
        catch (Exception ex)
        {
            await LogFile.AddLogMessageAsync("AddUserAsync", ex.Message, null, true);
        }
        return 0;
    }
    public static async Task<int> AddCategories(int id, string categoryName)//Добавление категории в БД(OP)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                using (SqlCommand command = new SqlCommand("AddCategoryProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
                    command.Parameters.Add(new SqlParameter("@CategoryName", SqlDbType.NVarChar, -1) { Value = categoryName });

                    SqlParameter paramResult = new SqlParameter("@Result", SqlDbType.Int) { Direction =  ParameterDirection.Output };
                    command.Parameters.Add(paramResult);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    return (int)paramResult.Value;
                }
            }
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("AddCategories", ex.Message, null, true);
        }
        return 0;
    }
    public static async Task<int> AddProductName(string article, int categoryId, string productName)//Добавление продукта в БД(OP)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("AddProductProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@Id", SqlDbType.NVarChar, 255) { Value = article });
                    command.Parameters.Add(new SqlParameter("@CategoryId", SqlDbType.Int) { Value = categoryId });
                    command.Parameters.Add(new SqlParameter("@ProductName", SqlDbType.NVarChar, 255) { Value = productName });

                    var resultParam = new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(resultParam);

                    await command.ExecuteNonQueryAsync();

                    return (int)resultParam.Value;
                }
            }
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("AddProductName", ex.Message, null, true);
        }
        return 0;
    }
    public static async Task<int> AddProductPrice(string productId, decimal price)//Добавление цены на продукт в БД(OP)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("AddProductPriceProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = productId });
                    command.Parameters.Add(new SqlParameter("@Price", SqlDbType.Decimal) { Value = price });
                    command.Parameters.Add(new SqlParameter("@PriceDate", SqlDbType.Date) { Value = DateTime.Now });

                    var resultParam = new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(resultParam);

                    await command.ExecuteNonQueryAsync();

                    return (int)resultParam.Value;
                }
            }
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("AddProductPrice", ex.Message, null, true);
        }
        return 0;
    }
    public static async Task<int> AddAttributeName(string attributeName)//Добавление названия характеристики в БД(OP)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("AddAttributeProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@AttributeName", SqlDbType.NVarChar, 255) { Value = attributeName });

                    var resultParam = new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(resultParam);

                    await command.ExecuteNonQueryAsync();

                    return (int)resultParam.Value;
                }
            }
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("AddAttributeName", ex.Message, null, true);
        }
        return 0;
    }
    public static async Task<int> AddAttributeValue(string productId, string attributeName, string value)//Добавление значений характеристик в БД(OP)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("AddAttributeValueProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = productId });
                    command.Parameters.Add(new SqlParameter("@AttributeName ", SqlDbType.NVarChar, 50) { Value = attributeName });
                    command.Parameters.Add(new SqlParameter("@Value", SqlDbType.VarChar, 50) { Value = value });

                    var resultParam = new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(resultParam);

                    await command.ExecuteNonQueryAsync();

                    return (int)resultParam.Value;
                }
            }
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("AddAttributeValue", ex.Message, null, true);
        }
        return 0;
    }
    #endregion Добавление данных

    #region Получение данных
    public static async Task<int> CheckUserAsync(string login)//Проверка существования пользователя в БД(OP)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                using (SqlCommand command = new SqlCommand("CheckUserProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@Login", login);

                    SqlParameter resultParameter = new SqlParameter("@Result", SqlDbType.Int);
                    resultParameter.Direction = ParameterDirection.Output;
                    command.Parameters.Add(resultParameter);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    return (int)resultParameter.Value;
                }
            }
        }
        catch (Exception ex)
        {
            await LogFile.AddLogMessageAsync("CheckUserAsync", ex.Message, null, true);
        }
        return 0;
    }
    public static async Task<string> GetUserHashPassword(string login)//Получение Хэш-пароля пользователя(OP)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                using (SqlCommand command = new SqlCommand("GetUserPasswordProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@Login", SqlDbType.NVarChar, -1) { Value = login });

                    SqlParameter paramResult = new SqlParameter("@Password", SqlDbType.NVarChar, -1) { Direction =  ParameterDirection.Output };
                    command.Parameters.Add(paramResult);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    return (string)paramResult.Value;
                }
            }

        }
        catch (Exception ex)
        {
            await LogFile.AddLogMessageAsync("GetUserHashPassword", ex.Message, null, true);
        }
        return string.Empty;
    }
    public static async Task<List<Category>> GetAllCategories(int categoryID = 0)//Получение всех категорий продуктов(OP)
    {
        List<Category> categories = new List<Category>();
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("GetAllCategoriesProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    if (categoryID > 0)
                    {
                        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = categoryID });
                    }

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new Category
                            {
                                Id = reader.GetInt32("Id"),
                                Name = reader.GetString("CategoryName")
                            });
                        }
                    }
                }
            }
            return categories;
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("GetAllCategories", ex.Message, null, true);
        }
        return null;
    }
    public static async Task<List<ProductName>> GetAllProducts()//Получение всех продуктов(ОР)
    {
        List<ProductName> products = new List<ProductName>();
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("GetAllProductsProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            products.Add(new ProductName
                            {
                                CategoryId = reader.GetInt32("CategoryId"),
                                Name = reader.GetString("ProductName"),
                                Article = reader.GetString("Id")
                            });
                        }
                    }
                }
            }
            return products;
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("GetAllProducts", ex.Message, null, true);
        }
        return null;
    }
    public static async Task<List<ProductAttribute>> GetAllAttributes()//Получение всех назваий всех характеристик(ОР)
    {
        List<ProductAttribute> productAttributes = new List<ProductAttribute>();
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("GetAllAttributtesProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            productAttributes.Add(new ProductAttribute
                            {
                                Id = reader.GetInt32("Id"),
                                Name = reader.GetString("AttributeName")
                            });
                        }
                    }
                }
            }
            return productAttributes;
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("GetAllAttributes", ex.Message, null, true);
            return null;
        }


    }
    public static async Task<List<AttributeValues>> GetAllAttributeValues()//Получение всех значений характеристик(ОР)
    {
        List<AttributeValues> attributeValues = new List<AttributeValues>();

        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("GetAllAttributeValuesProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            attributeValues.Add(new AttributeValues
                            {
                                AttributeId = reader.GetInt32("AttributeId"),
                                ProductId = reader.GetString("ProductId"),
                                Value = reader.GetString("Value")
                            });
                        }
                    }
                }
            }
            return attributeValues;
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("GetAllAttributeValues", ex.Message, null, true);
            return null;
        }

    }
    public static async Task<List<AveragePrice>> GetAveragePricesAsync()//Получение всех средних цен(OP)
    {
        List<AveragePrice> averagePrices = new List<AveragePrice>();
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("GetAllAveragePricesProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            averagePrices.Add(new AveragePrice
                            {
                                CategoryId = reader.GetInt32("CategoryId"),
                                Average_Price = reader.GetDecimal("AveragePrice"),
                                AveragePriceDate = reader.GetDateTime("PriceDate"),
                                CategoryName = (await GetAllCategories(reader.GetInt32("CategoryId"))).FirstOrDefault().Name
                            });
                        }
                    }
                }
            }
            return averagePrices;
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("GetAveragePricesAsync", ex.Message, null, true);
            return null;
        }

    }
    public static async Task<List<PriceIndex>> GetAllPriceIndexes()//Получение всех индексов цен(OP)
    {
        List<PriceIndex> priceIndexes = new List<PriceIndex>();
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("GetAllPriceIndexesProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            priceIndexes.Add(new PriceIndex
                            {
                                CategoryId = reader.GetInt32("CategoryId"),
                                IndexValue = reader.GetDecimal("IndexValue"),
                                IndexDateFrom = reader.GetDateTime("IndexDateFrom"),
                                IndexDateTo = reader.GetDateTime("IndexDateTo"),
                                CategoryName = (await GetAllCategories(reader.GetInt32("CategoryId"))).FirstOrDefault().Name
                            });
                        }
                    }
                }
            }
            return priceIndexes;
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("GetAllPriceIndexes", ex.Message, null, true);
            return null;
        }

    }
    public static async Task<List<ProductPrice>> GetAllPricesAsync()//Получение всех цен(ОР)
    {
        List<ProductPrice> productPrices = new List<ProductPrice>();
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("GetPricesByDateProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            productPrices.Add(new ProductPrice
                            {
                                Price = reader.GetDecimal("Price"),
                                ProductId = reader.GetString("ProductId")
                            });
                        }
                    }
                }
            }
            return productPrices;
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("GetAllPricesAsync", ex.Message, null, true);
            return null;
        }

    }
    public static async Task<DateTime?> GetLastDate()//Получение последней даты обновления(OP)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("GetLastPriceDate", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    var outputParam = new SqlParameter("@LastPriceDate", SqlDbType.Date) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(outputParam);

                    await command.ExecuteNonQueryAsync();

                    if (outputParam.Value != DBNull.Value) return (DateTime?)outputParam.Value;
                }
            }
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("GetLastDate", ex.Message, null, true);
        }

        return null;
    }
    public static async Task<DateTime?> GetFirstDate()//Получение первой даты обновления(OP)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("GetFirstPriceDate", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    var outputParam = new SqlParameter("@FirstPriceDate", SqlDbType.Date) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(outputParam);

                    await command.ExecuteNonQueryAsync();

                    if (outputParam.Value != DBNull.Value) return (DateTime?)outputParam.Value;
                }
            }
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("GetFirstDate", ex.Message, null, true);
        }

        return null;
    }
    #endregion Получение данных

    #region Расчет данных
    public static async Task<int> CalculateAveragePrices(int categoryId)//Рассчет и вставка данных в таблицу средних цен(ОР)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("CalculateAndInsertAveragePriceProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@CategoryId", SqlDbType.Int) { Value = categoryId });
                    command.Parameters.Add(new SqlParameter("@NeededDate", SqlDbType.Date) { Value = DateTime.Now.Date });

                    var resultParam = new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(resultParam);

                    await command.ExecuteNonQueryAsync();

                    return (int)resultParam.Value;
                }
            }
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("CalculateAveragePrices", ex.Message, null, true);
            return 0;
        }
    }
    public static async Task<int> CalculatePriceIndexes(int categoryId)//Рассчет и вставка данных в таблицу индексов потребительских цен(ОР)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString + "Database=PriceAnalysis;"))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("CalculateAndInsertPriceIndexProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@CategoryId", SqlDbType.Int) { Value = categoryId });

                    var resultParam = new SqlParameter("@InsertResult", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(resultParam);

                    await command.ExecuteNonQueryAsync();

                    return (int)resultParam.Value;
                }
            }
        }
        catch (SqlException ex)
        {
            await LogFile.AddLogMessageAsync("CalculatePriceIndexes", ex.Message, null, true);
            return 0;
        }

    }
    #endregion Расчет данных

    static async Task ExecuteSQLScriptNonQueryAsync()//Выполнение скрипта(OP)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                if (sqlScript.Contains("GO"))
                {
                    string[] batches = sqlScript.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var script in batches)
                    {
                        using (SqlCommand sqlCommand = new SqlCommand(script, connection))
                        {
                            await sqlCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
                else
                {
                    using (SqlCommand sqlCommand = new SqlCommand(sqlScript, connection))
                    {
                        await sqlCommand.ExecuteNonQueryAsync();
                    }
                }

            }
        }
        catch (Exception ex)
        {
            await LogFile.AddLogMessageAsync("ExecuteSQLScriptNonQueryAsync", ex.Message, null, true);
        }
    }
}
