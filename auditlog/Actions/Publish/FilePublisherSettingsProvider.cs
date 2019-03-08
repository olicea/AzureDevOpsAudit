using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace auditlog
{
    public interface IPublisherProvider
    {
        Task<List<PublishTargetSettings>> LoadPublishersSettingsAsync();
    }

    public class PublishTargetSettings
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public string Token { get; set; }

        public int BatchSize { get; } = 100;
    }

    public class TargetResult
    {
        public string Target { get; set; }

        public string Result { get; set; }

        public string Watermark { get; set; }
    }

    public class FilePublisherSettingsProvider : IPublisherProvider
    {
        public const string DefaultFileName = "publishSettings.json";

        public string FileName;


        public List<PublishTargetSettings> Settings { get; private set; }

        public FilePublisherSettingsProvider(string fileName = null)
        {
            FileName = fileName ?? DefaultFileName;
        }

        public async Task<List<PublishTargetSettings>> LoadPublishersSettingsAsync()
        {
            JsonSerializer serializer = JsonSerializer.CreateDefault();

            if (!File.Exists(FileName))
            {
                // The file does not exist creating an empty one and exit
                Settings = new List<PublishTargetSettings>()
                {
                    new PublishTargetSettings()
                    {
                        Name = nameof(PublishTargetSettings.Name),
                        Token = nameof(PublishTargetSettings.Token),
                        Url = nameof(PublishTargetSettings.Url)
                    }
                };

                using (StreamWriter fileStream = File.CreateText(FileName))
                using (JsonWriter jsonWriter = new JsonTextWriter(fileStream))
                {
                    serializer.Serialize(jsonWriter, Settings);
                    throw new InvalidTargetSettingsException(Resources.Publish_Settings_file_created + FileName);
                }
            }

            // The file is exists, try to deserialize the content
            using (StreamReader fileStream = new StreamReader(FileName))
            {
                string json = await fileStream.ReadToEndAsync();
                return JsonConvert.DeserializeObject<List<PublishTargetSettings>>(json);
            }
        }
    }
}
