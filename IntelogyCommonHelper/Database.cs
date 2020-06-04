using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace IntelogyCommonHelper
{
    public sealed class Database : IAsyncDisposable
    {
        private MySqlConnection _connection;
        private MySqlTransaction _transaction;

        public static string Address { get; set; }
        public static string Port { get; set; }
        public static string User { get; set; }
        public static string Password { get; set; }


        public Database(string database = "")
        {
            _connection = new MySqlConnection($"Server={Address};Port={Port};Uid={User};Pwd='{Password}';SslMode=none;Compress=true;ConvertZeroDateTime=true;" + (string.IsNullOrWhiteSpace(database) ? "" : $"Database={database}"));
            //var Settings = _connection.GetType().GetProperty("Settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_connection);            
                              
        }

        public async Task OpenAsync()
        {
            await _connection.OpenAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction == null)
            {
                _transaction = await _connection.BeginTransactionAsync();
            }
        }

        /// <summary>
        /// Executes a query inside a MySqlTransaction object and returns the value in position [0,0]
        /// </summary>
        /// <param name="query">The Query you wish to execute</param>
        /// <param name="parameters">The MySqlParameter you wish to attach to the query</param>
        /// <returns>The value in position [0,0]</returns>
        public async Task<T> GetDataSingleResultAsync<T>(string query, params MySqlParameter[] parameters)
        {
            MySqlConnection conn = await CheckConnectionValidAsync();
            using (MySqlCommand cmd = CreateCommand(conn, query, parameters))
            {
                return (T)(await cmd.ExecuteScalarAsync());
            }
        }

        /// <summary>
        /// Executes a query inside a MySqlTransaction object and returns the value in position [0,0]
        /// </summary>
        /// <param name="query">The Query you wish to execute</param>
        /// <param name="parameters">The MySqlParameter you wish to attach to the query</param>
        /// <returns>The value in position [0,0]</returns>
        public async Task<(bool pass, T outValue)> TryGetDataSingleResultAsync<T>(string query, params MySqlParameter[] parameters)
        {
            MySqlConnection conn = await CheckConnectionValidAsync();
            using (MySqlCommand cmd = CreateCommand(conn, query, parameters))
            {
                var obj = await cmd.ExecuteScalarAsync();
                if (obj == null)
                {                    
                    return (false, default(T));
                }
                else if (obj == DBNull.Value)
                {                    
                    return (true, default(T));
                }
                else
                {
                    T value;

                    if (obj.GetType() == typeof(T))
                    {
                        value = (T)obj;
                    }
                    else
                    {
                        value = (T)Convert.ChangeType(obj, typeof(T));
                    }
                    return (true, value);
                }
            }
        }
        
        public async Task<long> SetDataReturnLastInsertIdAsync(string query, params MySqlParameter[] parameters)
        {
            using (MySqlCommand cmd = await SetDataAsync(query, parameters))
            {
                await cmd.ExecuteNonQueryAsync();
                return cmd.LastInsertedId;
            }
        }

        public async Task<List<T>> ReadTransactionSeqAsync<T>(string query, Func<DataReader, T> selector, params MySqlParameter[] parameters)
        {
            MySqlConnection conn = await CheckConnectionValidAsync();
            using (MySqlCommand cmd = CreateCommand(conn, query, parameters))
            {
                cmd.CommandText = query;
                using (var r = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                {
                    var items = new List<T>();
                    var dr = new DataReader(r);

                    while (await r.ReadAsync())
                    {
                        items.Add(selector(dr));
                        dr.Index = 0;
                    }                        
                    return items;
                }
            }
        }




        public async Task<List<T>> ReadTransaction<T>(string query, Func<DataReader, T> selector, params MySqlParameter[] parameters)
        {
            MySqlConnection conn = await CheckConnectionValidAsync();
            using (MySqlCommand cmd = CreateCommand(conn, query, parameters))
            {
                cmd.CommandText = query;
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    var items = new List<T>();
                    var dr = new DataReader(r);

                    while (await r.ReadAsync())
                    {
                        items.Add(selector(dr));
                        dr.Index = 0;
                    }
                    return items;
                }
            }
        }

        public async IAsyncEnumerable<T> ReadTransactionEnumerable<T>(string query, Func<DataReader, T> selector, params MySqlParameter[] parameters)
        {
            MySqlConnection conn = await CheckConnectionValidAsync();
            using (MySqlCommand cmd = CreateCommand(conn, query, parameters))
            {
                cmd.CommandText = query;
                using (var r =  await cmd.ExecuteReaderAsync())
                {
                    var dr = new DataReader(r);

                    while (r.Read())
                    {
                        yield return selector(dr);
                        dr.Index = 0;
                    }
                        
                }
            }
        }

        public async Task<int> SetDataReturnRowCount(string query, params MySqlParameter[] parameters)
        {
            using (MySqlCommand cmd = await SetDataAsync(query, parameters))
            {
                int value = await cmd.ExecuteNonQueryAsync();
                return value;
            }
        }

        public async Task<bool> SetDataReturnNone(string query, params MySqlParameter[] parameters)
        {
            using (MySqlCommand cmd = await SetDataAsync(query, parameters))
            {
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
        }

        /// <summary>
        /// Executes a query inside a MySqlTransaction object and returns meta information about the query
        /// </summary>
        /// <param name="query">The Query you wish to execute</param>
        /// <param name="parameters">The MySqlParameter you wish to attach to the query</param>
        /// <returns>A long value containing info in relation to the returnInfo parameter</returns>
        public async Task<MySqlCommand> SetDataAsync(string query, params MySqlParameter[] parameters)
        {
            MySqlConnection conn = await CheckConnectionValidAsync();
            if (_transaction == null)
            {
                await BeginTransactionAsync();
            }
            return CreateCommand(conn, query, parameters);
        }
        private bool _commited = false;
        /// <summary>
        /// Commits the contents of the MySqlTransaction Object to the database.
        /// </summary>
        public async Task<bool> TransactionCommitAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                    _commited = true;
                }

                return true;
            }
            catch (Exception)
            {
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                    _transaction = null;
                }
                return false;
            }
        }

        public async Task TransactionRollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                _transaction = null;
            }

        }

        public async Task<MySqlConnection> CheckConnectionValidAsync()
        {
            MySqlConnection conn = _connection;
            if (conn.State != ConnectionState.Closed)
            {
                return conn;
            }

            await conn.OpenAsync();

            return conn;
        }

        public MySqlCommand CreateCommand(MySqlConnection conn, string query, params MySqlParameter[] parameters)
        {
            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = query,
                Connection = conn
            };

            if (_transaction != null)
                cmd.Transaction = _transaction;

            cmd.Parameters.AddRange(parameters);
            return cmd;
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                if (!_commited)
                {
                    await _transaction.RollbackAsync();
                }

                await _transaction.DisposeAsync();
            }

            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
