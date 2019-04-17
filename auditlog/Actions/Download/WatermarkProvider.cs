using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Audit.WebApi;

namespace auditlog
{
    public class AuditLogWatermark
    {
        public string Source {get; set;}
        public string Watermark {get; set;}
    }

    public interface IWatermarkProvider
    {
        AuditLogWatermark GetWatermark(string source);
        void SetWatermark(AuditLogWatermark watermark);
    }

    public class FileWatermark : IWatermarkProvider
    {
        public const string DefaultFileName = "auditLogWatermark.json";
        public string FileName;
        
        public AuditLogWatermark GetWatermark(string source)
        {
            JsonSerializer serializer = JsonSerializer.CreateDefault();
            List<AuditLogWatermark> watermarks;
            if (!File.Exists(FileName))
            {
                // The file does not exist creating an empty one and exit
                watermarks = new List<AuditLogWatermark>()
                {
                    new AuditLogWatermark()
                    {
                        Source = source,
                        Watermark  = null
                    }
                };

                using (StreamWriter fileStream = File.CreateText(FileName))
                using (JsonWriter jsonWriter = new JsonTextWriter(fileStream))
                {
                    serializer.Serialize(jsonWriter, watermarks);
                    //throw new InvalidTargetSettingsException(Resources.AuditLog_Watermark_file_created + FileName);
                }

                return watermarks[0];
            }
            else
            {
                // The file is exists, try to deserialize the content
                using (StreamReader fileStream = new StreamReader(FileName))
                {
                    string json = await fileStream.ReadToEndAsync();
                    watermarks = JsonConvert.DeserializeObject<List<AuditLogWatermark>>(json);
                }

                return watermarks.FirstOrDefault(w => string.Equals(source, w.Source, StringComparison.OrdinalIgnoreCase));
            }
        }

        public void SetWatermark(AuditLogWatermark value)
        {

        }
    }
}
