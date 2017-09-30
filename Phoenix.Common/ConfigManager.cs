using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Specialized;

namespace Phoenix.Common
{
    public class ConfigManager
    {
        private static string _defaultCfgTable = "MasterConfig";
        CloudTable _cfgCloudTable = null;
        private string _cfgTableName;
        private string _componentName;
        private bool _isInitialized;
        private NameValueCollection _appSettings;
        public NameValueCollection AppSettings
        {
            get { return _appSettings; }
        }

        public ConfigManager()
        {
            _cfgTableName = _defaultCfgTable;
        }

        public ConfigManager(string cfgTableName)
        {
            if (string.IsNullOrEmpty(cfgTableName))
            {
                throw new ArgumentException("Config table name cannot be empty. Use default constructor instead.", "cfgTableName");
            }
            _cfgTableName = cfgTableName;
        }

        public bool Init(string azureConnectionStr, string componentName)
        {
            _isInitialized = false;

            if (string.IsNullOrEmpty(azureConnectionStr))
            {
                throw new ArgumentException("Azure connection string parameter cannot be empty.", "azureConnectionStr");
            }
            if (string.IsNullOrEmpty(componentName))
            {
                throw new ArgumentException("Component name cannot be empty.", "componentName");
            }

            _componentName = componentName;
            _appSettings = new NameValueCollection();

            // initialize table access and make sure table exists
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(azureConnectionStr);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            _cfgCloudTable = tableClient.GetTableReference(_cfgTableName);

            _cfgCloudTable.CreateIfNotExists();

            // validate if table exists
            if (!_cfgCloudTable.Exists())
            {
                throw new InvalidOperationException("Config table could not be created.");
            }
            // read all the values
            read();
            _isInitialized = true;
            return _isInitialized;
        }

        public bool AddOrUpdate(string key, string configValue)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Not initialized. Call Init() to initialize.");
            }

            // retrieve 
            TableOperation retrieveOperation = TableOperation.Retrieve<ConfigEntity>(_componentName, key);
            TableResult retrievedResult = _cfgCloudTable.Execute(retrieveOperation);
            ConfigEntity existingEntity = (ConfigEntity)retrievedResult.Result;

            if (existingEntity != null)
            {
                // update the value
                existingEntity.ConfigValue = configValue;
                TableOperation tableOprn = TableOperation.Merge(existingEntity);
                _cfgCloudTable.Execute(tableOprn);

            }
            else
            {
                // insert
                existingEntity = new ConfigEntity { PartitionKey = _componentName, RowKey = key, ConfigValue = configValue };
                TableOperation tableOprn = TableOperation.Insert(existingEntity);
                _cfgCloudTable.Execute(tableOprn);
            }

            // reload 
            read();

            return true;
        }

        private void read()
        {
            // Create the table query.
            TableQuery<ConfigEntity> rangeQuery = new TableQuery<ConfigEntity>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, _componentName));

            var queryResult = _cfgCloudTable.ExecuteQuery(rangeQuery);
            // read from table and assign to local array
            _appSettings.Clear();
            foreach (ConfigEntity entity in queryResult)
            {
                _appSettings.Add(entity.RowKey, entity.ConfigValue);
            }

        }
    }

    internal class ConfigEntity : TableEntity
    {
        public ConfigEntity(string componentName, string configKey)
        {
            // split the key into two to speed up access by component name (ex: SyncAlarm)
            PartitionKey = componentName;
            RowKey = configKey;
        }
        public ConfigEntity() { }
        public string ConfigValue { get; set; }
    }
}
