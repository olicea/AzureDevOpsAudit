using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace auditlog
{
    public interface IAuditLogSettingsProvider
    {
        Task<AuditLogSettings> GetSettingsAsync();
    }

    public class AuditLogSettings
    {
        public string Url { get; set; }
        public string PAT { get; set; }
    }

    internal class FileAuditLogSettingsProvider : IAuditLogSettingsProvider
    {
        public const string DefaultFileName = "auditLogSettings.json";

        public async Task<AuditLogSettings> GetSettingsAsync()
        {
            JsonSerializer serializer = JsonSerializer.CreateDefault();

            if (!File.Exists(DefaultFileName))
            {
                // The file does not exist creating an empty one and exit
                var settings = new AuditLogSettings()
                {
                    Url = nameof(AuditLogSettings.Url),
                    PAT = nameof(AuditLogSettings.PAT)
                };

                using (StreamWriter fileStream = File.CreateText(DefaultFileName))
                using (JsonWriter jsonWriter = new JsonTextWriter(fileStream))
                {
                    serializer.Serialize(jsonWriter, settings);
                    throw new InvalidTargetSettingsException(Resources.Publish_Settings_file_created + DefaultFileName);
                }
            }

            // The file is exists, try to deserialize the content
            using (StreamReader fileStream = new StreamReader(DefaultFileName))
            {
                string json = await fileStream.ReadToEndAsync();
                return JsonConvert.DeserializeObject<AuditLogSettings>(json);
            }
        }
    }
}
