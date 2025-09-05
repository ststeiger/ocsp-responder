
namespace ReportServerProxyFF
{


    public class SQL
    {

        protected static System.Data.Common.DbProviderFactory m_ProviderFactory = InitFactory();


        public static System.Data.Common.DbProviderFactory ProviderFactory
        {
            get
            {
                return m_ProviderFactory;
            }
        }


        protected static System.Data.Common.DbProviderFactory InitFactory()
        {
            return InitFactory("System.Data.SqlClient");
        } // End Function InitFactory


        protected static System.Data.Common.DbProviderFactory InitFactory(string strProviderName)
        {
            if (string.IsNullOrEmpty(strProviderName))
            {
                strProviderName = "System.Data.SqlClient";
            }

            if (System.StringComparer.OrdinalIgnoreCase.Equals(strProviderName, "Npgsql"))
            {
                m_ProviderFactory = GetPostGreFactory();
                return m_ProviderFactory;
            }


            m_ProviderFactory = System.Data.Common.DbProviderFactories.GetFactory("System.Data.SqlClient");
            return m_ProviderFactory;
        } // End Function InitFactory


        protected static System.Data.Common.DbProviderFactory GetPostGreFactory()
        {
            //AddFactoryClasses();
            System.Data.Common.DbProviderFactory providerFactory = null;
            //providerFactory = GetFactory(GetType(Npgsql.NpgsqlFactory).AssemblyQualifiedName)
            //providerFactory = GetFactory(typeof(Npgsql.NpgsqlFactory));

            return providerFactory;
        } // End Function GetPostGreFactory


        protected static System.Data.Common.DbProviderFactory GetFactory(System.Type tFactoryType)
        {
            if (tFactoryType != null && tFactoryType.IsSubclassOf(typeof(System.Data.Common.DbProviderFactory)))
            {
                // Provider factories are singletons with Instance field having the sole instance
                System.Reflection.FieldInfo field = tFactoryType.GetField("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null)
                {
                    //return field.GetValue(null) as DbProviderFactory;
                    return (System.Data.Common.DbProviderFactory)field.GetValue(null);
                }
            }

            throw new System.Configuration.ConfigurationErrorsException("DataProvider is missing!");
        } // End Function GetFactory





        public static System.Data.Common.DbConnectionStringBuilder GetConnectionStringBuilder(string strConnectionString)
        {
            System.Data.Common.DbConnectionStringBuilder dbConString = m_ProviderFactory.CreateConnectionStringBuilder();
            dbConString.ConnectionString = strConnectionString;

            return dbConString;
        } // End Functin GetConnectionStringBuilder


        public static string GetConnectionString()
        {
            return GetConnectionString(null);
        } // End Function GetConnectionString


        protected static string strStaticConnectionString = null;
        public static string GetConnectionString(string strIntitialCatalog)
        {
            string strReturnValue = null;
            string strProviderName = null;


            if (string.IsNullOrEmpty(strStaticConnectionString))
            {
                string strConnectionStringName = System.Environment.MachineName;

                if (string.IsNullOrEmpty(strConnectionStringName))
                {
                    strConnectionStringName = "LocalSqlServer";
                }

                System.Configuration.ConnectionStringSettingsCollection settings = System.Configuration.ConfigurationManager.ConnectionStrings;
                if ((settings != null))
                {
                    foreach (System.Configuration.ConnectionStringSettings cs in settings)
                    {
                        if (System.StringComparer.OrdinalIgnoreCase.Equals(cs.Name, strConnectionStringName))
                        {
                            strReturnValue = cs.ConnectionString;
                            strProviderName = cs.ProviderName;
                            break; // TODO: might not be correct. Was : Exit For
                        }
                    }
                }

                if (string.IsNullOrEmpty(strReturnValue))
                {
                    strConnectionStringName = "server";

                    System.Configuration.ConnectionStringSettings conString = System.Configuration.ConfigurationManager.ConnectionStrings[strConnectionStringName];

                    if (conString != null)
                    {
                        strReturnValue = conString.ConnectionString;
                    }
                }

                if (string.IsNullOrEmpty(strReturnValue))
                {
                    throw new System.ArgumentNullException("ConnectionString \"" + strConnectionStringName + "\" in file web.config.");
                }

                settings = null;
                strConnectionStringName = null;
            }
            else
            {
                if (string.IsNullOrEmpty(strIntitialCatalog))
                {
                    return strStaticConnectionString;
                }

                strReturnValue = strStaticConnectionString;
            }

            InitFactory(strProviderName);
            System.Data.Common.DbConnectionStringBuilder sb = GetConnectionStringBuilder(strReturnValue);


            if (string.IsNullOrEmpty(strStaticConnectionString))
            {
                if (!System.Convert.ToBoolean(sb["Integrated Security"]))
                {
                    sb["Password"] = DES.DeCrypt(System.Convert.ToString(sb["Password"]));
                }
                strReturnValue = sb.ConnectionString;
                strStaticConnectionString = strReturnValue;
            }


            if (!string.IsNullOrEmpty(strIntitialCatalog))
            {
                sb["Database"] = strIntitialCatalog;
            }


            strReturnValue = sb.ConnectionString;
            sb = null;

            return strReturnValue;
        } // End Function GetConnectionString


        public static string GetInitialCatalog()
        {
            string cs = GetConnectionString();
            System.Data.SqlClient.SqlConnectionStringBuilder csb = new System.Data.SqlClient.SqlConnectionStringBuilder(cs);

            string strInitialCatalog = csb.InitialCatalog;
            csb.Clear();

            return strInitialCatalog;
        }


        public static System.Data.Common.DbConnection GetConnection()
        {
            return GetConnection(null);
        } // End Function GetConnection


        public static System.Data.Common.DbConnection GetConnection(string strConnectionString)
        {
            if (string.IsNullOrEmpty(strConnectionString))
            {
                strConnectionString = GetConnectionString();
            }

            System.Data.Common.DbConnection idbcon = m_ProviderFactory.CreateConnection();
            idbcon.ConnectionString = strConnectionString;

            return idbcon;
        } // End Function GetConnection


        public static System.Data.Common.DbCommand CreateCommand(string strSQL, int timeout)
        {
            System.Data.Common.DbCommand idbc = m_ProviderFactory.CreateCommand();


            if (!string.IsNullOrEmpty(strSQL))
            {
                idbc.CommandText = strSQL;
            }

            idbc.CommandTimeout = System.Math.Max(timeout, 0);

            return idbc;
        } // End Function CreateCommand


        public static System.Data.Common.DbCommand CreateCommand(string strSQL)
        {
            return CreateCommand(strSQL, 30);
        }


        public static void RemoveParameter(System.Data.IDbCommand command, string parameterName)
        {
            if (!parameterName.StartsWith("@"))
            {
                parameterName = "@" + parameterName;
            }

            if ((command.Parameters.Contains(parameterName)))
            {
                command.Parameters.RemoveAt(parameterName);
            }
        }


        public static System.Data.IDbDataParameter SetParameter(System.Data.IDbCommand command, string strParameterName, object objValue)
        {
            return AddParameter(true, command, strParameterName, objValue);
        }


        public static System.Data.IDbDataParameter AddParameter(System.Data.IDbCommand command, string strParameterName, object objValue)
        {
            return AddParameter(false, command, strParameterName, objValue);
        }


        public static System.Data.IDbDataParameter AddParameter(bool allowOverride, System.Data.IDbCommand command, string strParameterName, object objValue)
        {
            return AddParameter(allowOverride, command, strParameterName, objValue, System.Data.ParameterDirection.Input);
        } // End Function AddParameter


        public static System.Data.IDbDataParameter SetParameter(System.Data.IDbCommand command, string strParameterName, object objValue, System.Data.ParameterDirection pad)
        {
            return AddParameter(true, command, strParameterName, objValue, pad);
        }


        public static System.Data.IDbDataParameter AddParameter(System.Data.IDbCommand command, string strParameterName, object objValue, System.Data.ParameterDirection pad)
        {
            return AddParameter(false, command, strParameterName, objValue, pad);
        }


        public static System.Data.IDbDataParameter AddParameter(bool allowOverride, System.Data.IDbCommand command, string strParameterName, object objValue, System.Data.ParameterDirection pad)
        {
            if (objValue == null)
            {
                //throw new System.ArgumentNullException("objValue");
                objValue = System.DBNull.Value;
            }
            // End if (objValue == null)
            System.Type tDataType = objValue.GetType();
            System.Data.DbType dbType = GetDbType(tDataType);

            return AddParameter(allowOverride, command, strParameterName, objValue, pad, dbType);
        } // End Function AddParameter


        public static System.Data.IDbDataParameter SetParameter(System.Data.IDbCommand command, string strParameterName, object objValue, System.Data.ParameterDirection pad, System.Data.DbType dbType)
        {
            return AddParameter(true, command, strParameterName, objValue, pad, dbType);
        }


        public static System.Data.IDbDataParameter AddParameter(System.Data.IDbCommand command, string strParameterName, object objValue, System.Data.ParameterDirection pad, System.Data.DbType dbType)
        {
            return AddParameter(false, command, strParameterName, objValue, pad, dbType);
        }


        private static System.Data.IDbDataParameter AddParameter(bool allowOverride, System.Data.IDbCommand command, string strParameterName, object objValue, System.Data.ParameterDirection pad, System.Data.DbType dbType)
        {
            System.Data.IDbDataParameter parameter = command.CreateParameter();

            if (!strParameterName.StartsWith("@"))
            {
                strParameterName = "@" + strParameterName;
            }

            if (command.Parameters.Contains(strParameterName)) command.Parameters.RemoveAt(strParameterName);

            if (allowOverride || !command.Parameters.Contains(strParameterName))
            {
                // End if (!strParameterName.StartsWith("@"))
                parameter.ParameterName = strParameterName;
                parameter.DbType = dbType;
                parameter.Direction = pad;


                if (objValue == null)
                {
                    parameter.Value = System.DBNull.Value;
                }
                else
                {
                    parameter.Value = objValue;
                }

                command.Parameters.Add(parameter);
                return parameter;
            }

            return null;
        } // End Function AddParameter


        protected static string SqlTypeFromDbType(System.Data.DbType type)
        {
            string strRetVal = null;

            // http://msdn.microsoft.com/en-us/library/cc716729.aspx
            switch (type)
            {
                case System.Data.DbType.Guid:
                    strRetVal = "uniqueidentifier";
                    break;
                case System.Data.DbType.Date:
                    strRetVal = "date";
                    break;
                case System.Data.DbType.Time:
                    strRetVal = "time(7)";
                    break;
                case System.Data.DbType.DateTime:
                    strRetVal = "datetime";
                    break;
                case System.Data.DbType.DateTime2:
                    strRetVal = "datetime2";
                    break;
                case System.Data.DbType.DateTimeOffset:
                    strRetVal = "datetimeoffset(7)";
                    break;

                case System.Data.DbType.StringFixedLength:
                    strRetVal = "nchar(MAX)";
                    break;
                case System.Data.DbType.String:
                    strRetVal = "nvarchar(MAX)";
                    break;

                case System.Data.DbType.AnsiStringFixedLength:
                    strRetVal = "char(MAX)";
                    break;
                case System.Data.DbType.AnsiString:
                    strRetVal = "varchar(MAX)";
                    break;

                case System.Data.DbType.Single:
                    strRetVal = "real";
                    break;
                case System.Data.DbType.Double:
                    strRetVal = "float";
                    break;
                case System.Data.DbType.Decimal:
                    strRetVal = "decimal(19, 5)";
                    break;
                case System.Data.DbType.VarNumeric:
                    strRetVal = "numeric(19, 5)";
                    break;

                case System.Data.DbType.Boolean:
                    strRetVal = "bit";
                    break;
                case System.Data.DbType.SByte:
                case System.Data.DbType.Byte:
                    strRetVal = "tinyint";
                    break;
                case System.Data.DbType.Int16:
                    strRetVal = "smallint";
                    break;
                case System.Data.DbType.Int32:
                    strRetVal = "int";
                    break;
                case System.Data.DbType.Int64:
                    strRetVal = "bigint";
                    break;
                case System.Data.DbType.Xml:
                    strRetVal = "xml";
                    break;
                case System.Data.DbType.Binary:
                    strRetVal = "varbinary(MAX)"; // or image
                    break;
                case System.Data.DbType.Currency:
                    strRetVal = "money";
                    break;
                case System.Data.DbType.Object:
                    strRetVal = "sql_variant";
                    break;

                case System.Data.DbType.UInt16:
                case System.Data.DbType.UInt32:
                case System.Data.DbType.UInt64:
                    throw new System.NotImplementedException("Uints not mapped - MySQL only");
            } // End switch (type)

            return strRetVal;
        } // End Function SqlTypeFromDbType


        protected static System.Data.DbType GetDbType(System.Type type)
        {
            // http://social.msdn.microsoft.com/Forums/en/winforms/thread/c6f3ab91-2198-402a-9a18-66ce442333a6
            string strTypeName = type.Name;
            System.Data.DbType DBtype = System.Data.DbType.String;
            // default value

            try
            {
                if (object.ReferenceEquals(type, typeof(System.DBNull)))
                {
                    return DBtype;
                }

                if (object.ReferenceEquals(type, typeof(System.Byte[])))
                {
                    return System.Data.DbType.Binary;
                }

                DBtype = (System.Data.DbType)System.Enum.Parse(typeof(System.Data.DbType), strTypeName, true);
                // add error handling to suit your taste
            }
            catch (System.Exception exNoMappingPresent)
            {
                // LogError("claSQL.cs ==> SQL.GetDbType", exNoMappingPresent, null);
                throw;
            }

            return DBtype;
        } // End Function GetDbType

        protected static T InlineTypeAssignHelper<T>(object UTO)
        {
            if (UTO == null)
            {
                T NullSubstitute = default(T);
                return NullSubstitute;
            }
            return (T)UTO;
        } // End Template InlineTypeAssignHelper



        public static T ExecuteScalar<T>(System.Data.IDbCommand cmd)
        {
            string strReturnValue = null;
            System.Type tReturnType = null;
            object objReturnValue = null;

            lock (cmd)
            {

                using (System.Data.IDbConnection idbc = GetConnection())
                {
                    cmd.Connection = idbc;

                    lock (cmd.Connection)
                    {

                        try
                        {
                            tReturnType = typeof(T);

                            if (cmd.Connection.State != System.Data.ConnectionState.Open)
                                cmd.Connection.Open();

                            objReturnValue = cmd.ExecuteScalar();

                            if (objReturnValue != null)
                            {

                                if (!object.ReferenceEquals(tReturnType, typeof(System.Byte[])))
                                {
                                    strReturnValue = objReturnValue.ToString();
                                } // End if (!object.ReferenceEquals(tReturnType, typeof(System.Byte[])))

                            } // End if (objReturnValue != null)

                        } // End Try
                        catch (System.Data.Common.DbException ex)
                        {
                            // LogError("claSQL.cs ==> SQL.ExecuteScalar", ex, cmd);
                            throw;
                        } // End Catch
                        finally
                        {
                            if (cmd.Connection.State != System.Data.ConnectionState.Closed)
                                cmd.Connection.Close();
                        } // End Finally

                    } // End lock (cmd.Connection)

                } // End using idbc

            } // End lock (cmd)


            try
            {

                if (object.ReferenceEquals(tReturnType, typeof(object)))
                {
                    return InlineTypeAssignHelper<T>(objReturnValue);
                }
                else if (object.ReferenceEquals(tReturnType, typeof(string)))
                {
                    return InlineTypeAssignHelper<T>(strReturnValue);
                } // End if string
                else if (object.ReferenceEquals(tReturnType, typeof(bool)))
                {
                    bool bReturnValue = false;
                    bool bSuccess = bool.TryParse(strReturnValue, out bReturnValue);

                    if (bSuccess)
                        return InlineTypeAssignHelper<T>(bReturnValue);

                    if (strReturnValue == "0")
                        return InlineTypeAssignHelper<T>(false);

                    return InlineTypeAssignHelper<T>(true);
                } // End if bool
                else if (object.ReferenceEquals(tReturnType, typeof(int)))
                {
                    if (string.IsNullOrEmpty(strReturnValue))
                        return InlineTypeAssignHelper<T>(0);

                    int iReturnValue = int.Parse(strReturnValue);
                    return InlineTypeAssignHelper<T>(iReturnValue);
                } // End if int
                else if (object.ReferenceEquals(tReturnType, typeof(uint)))
                {
                    if (string.IsNullOrEmpty(strReturnValue))
                        return InlineTypeAssignHelper<T>(0);

                    uint uiReturnValue = uint.Parse(strReturnValue);
                    return InlineTypeAssignHelper<T>(uiReturnValue);
                } // End if uint
                else if (object.ReferenceEquals(tReturnType, typeof(long)))
                {
                    if (string.IsNullOrEmpty(strReturnValue))
                        return InlineTypeAssignHelper<T>(0);

                    long lngReturnValue = long.Parse(strReturnValue);
                    return InlineTypeAssignHelper<T>(lngReturnValue);
                } // End if long
                else if (object.ReferenceEquals(tReturnType, typeof(ulong)))
                {
                    if (string.IsNullOrEmpty(strReturnValue))
                        return InlineTypeAssignHelper<T>(0);

                    ulong ulngReturnValue = ulong.Parse(strReturnValue);
                    return InlineTypeAssignHelper<T>(ulngReturnValue);
                } // End if ulong
                else if (object.ReferenceEquals(tReturnType, typeof(float)))
                {
                    if (string.IsNullOrEmpty(strReturnValue))
                        return InlineTypeAssignHelper<T>(0.0);

                    float fltReturnValue = float.Parse(strReturnValue);
                    return InlineTypeAssignHelper<T>(fltReturnValue);
                }
                else if (object.ReferenceEquals(tReturnType, typeof(double)))
                {
                    if (string.IsNullOrEmpty(strReturnValue))
                        return InlineTypeAssignHelper<T>(0.0);

                    double dblReturnValue = double.Parse(strReturnValue);
                    return InlineTypeAssignHelper<T>(dblReturnValue);
                }
                else if (object.ReferenceEquals(tReturnType, typeof(decimal)))
                {
                    if (string.IsNullOrEmpty(strReturnValue))
                        return InlineTypeAssignHelper<T>(0.0m);

                    decimal dblReturnValue = decimal.Parse(strReturnValue);
                    return InlineTypeAssignHelper<T>(dblReturnValue);
                }
                else if (object.ReferenceEquals(tReturnType, typeof(decimal?)))
                {
                    if (string.IsNullOrEmpty(strReturnValue))
                        return InlineTypeAssignHelper<T>(null);

                    decimal? dblReturnValue = decimal.Parse(strReturnValue);
                    return InlineTypeAssignHelper<T>(dblReturnValue);
                }
                else if (object.ReferenceEquals(tReturnType, typeof(System.Net.IPAddress)))
                {
                    System.Net.IPAddress ipaAddress = null;

                    if (string.IsNullOrEmpty(strReturnValue))
                        return InlineTypeAssignHelper<T>(ipaAddress);

                    ipaAddress = System.Net.IPAddress.Parse(strReturnValue);
                    return InlineTypeAssignHelper<T>(ipaAddress);
                } // End if IPAddress
                else if (object.ReferenceEquals(tReturnType, typeof(System.Byte[])))
                {
                    if (objReturnValue == System.DBNull.Value)
                        return InlineTypeAssignHelper<T>(null);

                    return InlineTypeAssignHelper<T>(objReturnValue);
                }
                else if (object.ReferenceEquals(tReturnType, typeof(System.Guid)))
                {
                    if (string.IsNullOrEmpty(strReturnValue)) return InlineTypeAssignHelper<T>(null);

                    return InlineTypeAssignHelper<T>(new System.Guid(strReturnValue));
                } // End if GUID
                else if (object.ReferenceEquals(tReturnType, typeof(System.DateTime)))
                {
                    System.DateTime bReturnValue = System.DateTime.Now;
                    bool bSuccess = System.DateTime.TryParse(strReturnValue, out bReturnValue);

                    if (bSuccess)
                        return InlineTypeAssignHelper<T>(bReturnValue);

                    if (strReturnValue == "0")
                        return InlineTypeAssignHelper<T>(false);

                    return InlineTypeAssignHelper<T>(true);
                } // End if datetime
                else // No datatype matches
                {
                    throw new System.NotImplementedException("ExecuteScalar<T>: This type is not yet defined.");
                } // End else of if tReturnType = datatype


            } // End Try
            catch (System.Exception ex)
            {
                // LogError("claSQL.cs ==> SQL.ExecuteScalar (2)", ex, cmd);
                throw;
            } // End Catch

            //return InlineTypeAssignHelper<T>(null);
        } // End Function ExecuteScalar(cmd)


        public static T ExecuteScalar<T>(string strSQL)
        {
            T tReturnValue = default(T);

            using (System.Data.IDbCommand cmd = CreateCommand(strSQL))
            {
                tReturnValue = ExecuteScalar<T>(cmd);
            } // End Using cmd

            return tReturnValue;
        } // End Function ExecuteScalar


        public static T ExecuteScalarFromFile<T>(string fileName)
        {
            string sql = ResourceHelper.ReadEmbeddedFileEndingWith(fileName);

            return ExecuteScalar<T>(sql);
        }


        public static System.Data.IDataReader ExecuteReader(System.Data.IDbCommand cmd)
        {
            System.Data.IDataReader dr = null;

            System.Data.IDbConnection sqldbConnection = null;


            lock (cmd)
            {
                try
                {
                    sqldbConnection = GetConnection();

                    bool bSuccess = System.Threading.Monitor.TryEnter(sqldbConnection, 5000);
                    if (!bSuccess)
                    {
                        throw new System.Exception("Could not get lock on SQL DB connection in COR.SQL.ExecuteReader ==> Threading.Monitor.TryEnter");
                    }

                    cmd.Connection = sqldbConnection;

                    if (!(cmd.Connection.State == System.Data.ConnectionState.Open))
                    {
                        cmd.Connection.Open();
                    }

                    dr = cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
                }
                catch (System.Exception ex)
                {
                    if (cmd != null && !(cmd.Connection.State == System.Data.ConnectionState.Closed))
                    {
                        cmd.Connection.Close();
                    }

                    // LogError("claSQL.cs ==> SQL.ExecuteReader", ex, cmd);
                    throw;
                }

            } // cmd

            return dr;
        } // End Function ExecuteReader


        public static System.Data.IDataReader ExecuteReader(string strSQL)
        {
            System.Data.IDataReader dr = null;

            using (System.Data.IDbCommand cmd = CreateCommand(strSQL))
            {
                dr = ExecuteReader(cmd);
            }

            return dr;
        } // End Function ExecuteReader


    }
}