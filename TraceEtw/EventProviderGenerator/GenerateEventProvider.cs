using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace EventProviderGenerator
{

    public class GenerateEventProvider : Task
    {
        string m_filename;
        string m_safeProviderName;

        [Required]
        public string InputXmlPath { get; set; }
        
        [Required]
        public string OutputDir { get; set; }

        [Required]
        public string WindowsSDK_ExecutablePath { get; set; }

        public bool Verbose { get; set; }

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Normal, "InputXmlPath: {0}", InputXmlPath);
            Log.LogMessage(MessageImportance.Normal, "OutputDir: {0}", OutputDir);
            Log.LogMessage(MessageImportance.Normal, "WindowsSDK_ExecutablePath: {0}", WindowsSDK_ExecutablePath);
            Log.LogMessage(MessageImportance.Normal, "Verbose: {0}", Verbose);

            m_filename = Path.GetFileNameWithoutExtension(InputXmlPath);
            Log.LogMessage(MessageImportance.Normal, "Filename: {0}", m_filename);

            Log.LogMessage(MessageImportance.Normal, "Loading input XML");
            var input = LoadInputXml();

            m_safeProviderName = GetSafeString(input.Name);

            Log.LogMessage(MessageImportance.Normal, "Creating output folder");
            var file = new FileInfo(OutputDir);
            file.Directory.Create();

            Log.LogMessage(MessageImportance.Normal, "Generating manifest");
            string xmlManifest = GenerateManifest(input);

            Log.LogMessage(MessageImportance.Normal, "Generating base header");
            if (!GenerateBaseHeader())
            {
                return false;
            }

            Log.LogMessage(MessageImportance.Normal, "Generating header");
            GenerateHeader(input, xmlManifest);

            return true;
        }

        private bool GenerateBaseHeader()
        {
            var args = System.String.Format(
                    @"-um {3} -h ""{1}\"" -r ""{1}\"" -z {2} ""{0}""",
                    Path.GetFullPath(OutputDir + m_filename + ".man"),
                    Path.GetFullPath(OutputDir),
                    m_filename + "Base",
                    Verbose ? "-v" : ""
                    );
            Log.LogMessage(MessageImportance.Normal, "Command line: mc.exe {0}", args);

            var procStartInfo = new ProcessStartInfo("mc.exe", args);
            procStartInfo.EnvironmentVariables["PATH"] += ";" + WindowsSDK_ExecutablePath;
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;

            var proc = new Process();
            proc.StartInfo = procStartInfo;
            proc.Start();
            proc.WaitForExit();
            var errorMessage = proc.StandardError.ReadToEnd();
            if (errorMessage.Length > 0)
            {
                Log.LogError("mc.exe: {0}", errorMessage);
                return false;
            }

            return true;
        }

        private EventProvider LoadInputXml()
        {
            var serializer = new XmlSerializer(typeof(EventProvider));
            var input = (EventProvider)serializer.Deserialize(new FileStream(InputXmlPath, FileMode.Open));

            if (string.IsNullOrEmpty(input.Guid))
            {
                input.Guid = GetGuidFromName(input.Name).ToString("B"); // Format: {00000000-0000-0000-0000-000000000000} 
            }

            return input;
        }

        private string GenerateManifest(EventProvider input)
        {
            var events = new StringBuilder();
            var tasks = new StringBuilder();
            var channels = new StringBuilder();
            var localizations = new Dictionary<string,string>();
            var templates = new StringBuilder();

            int eventId = 1;
            int taskId = 1;
            int channelId = 16; // Channel user values must be in the range from 16 through 255

            foreach (var item in input.Items)
            {
                var e = item as EventProviderEvent;
                var t = item as EventProviderTask;
                var c = item as EventProviderChannel;
                if (e != null)
                {
                    var templateId = HandleEventArgs(templates, eventId, e.Items);

                    var symbolName = $"{m_safeProviderName}_{e.Name}";
                    var localizationName = $"{symbolName}_message";
                    var messageAttr = "";
                    if (e.Message != null)
                    {
                        localizations.Add(localizationName, e.Message);
                        messageAttr = $@"message=""$(string.{localizationName})""";
                    }

                    events.AppendFormat(@"
          <event value=""{0}"" symbol=""{4}_{1}"" task=""{1}"" opcode=""win:Info"" level=""win:{2}"" {3}{5}{6}/>",
                        eventId,
                        e.Name,
                        e.Level,
                        templateId == null ? "" : @"template=""" + templateId + @""" ",
                        m_safeProviderName,
                        e.Channel == null ? "" : @"channel=""" + e.Channel + @""" ",
                        messageAttr
                        );
                    eventId++;

                    tasks.AppendFormat(@"
          <task value=""{0}"" name=""{1}"" />",
                        taskId,
                        e.Name
                        );
                    taskId++;
                }
                else if (t != null)
                {
                    var templateId = HandleEventArgs(templates, eventId, t.Start);

                    events.AppendFormat(@"
          <event value=""{0}"" symbol=""{4}_{1}_Start"" task=""{1}"" opcode=""win:Start"" level=""win:{2}"" {3}/>",
                        eventId,
                        t.Name,
                        t.Level,
                        templateId == null ? "" : @"template=""" + templateId + @""" ",
                        m_safeProviderName
                        );
                    eventId++;

                    templateId = HandleEventArgs(templates, eventId, t.Stop);

                    events.AppendFormat(@"
          <event value=""{0}"" symbol=""{4}_{1}_Stop"" task=""{1}"" opcode=""win:Stop"" level=""win:{2}"" {3}/>",
                        eventId,
                        t.Name,
                        t.Level,
                        templateId == null ? "" : @"template=""" + templateId + @""" ",
                        m_safeProviderName
                        );
                    eventId++;

                    tasks.AppendFormat(@"
          <task value=""{0}"" name=""{1}"" />",
                        taskId,
                        t.Name
                        );
                    taskId++;
                }
                else if (c != null)
                {
                    var symbolName = $"{m_safeProviderName}_{c.Id}";
                    var localizationName = $"{symbolName}_message";
                    localizations.Add(localizationName, c.Message);

                    channels.AppendFormat($@"
          <channel value=""{channelId}"" chid=""{c.Id}"" name=""{c.Name}"" symbol=""{symbolName}"" type=""{c.Type}"" enabled=""{c.Enabled.ToString().ToLowerInvariant()}"" message=""$(string.{localizationName})"" />");

                    channelId++;
                }
                else
                {
                    throw new InvalidDataException(System.String.Format("Invalid event type: {0}", item.GetType()));
                }
            }

            var stringTable = new StringBuilder();
            foreach (var loc in localizations)
            {
                stringTable.AppendFormat($@"
        <string id=""{loc.Key}"" value=""{loc.Value}"" />");
            }

            var manifest = new StringBuilder();
            manifest.AppendFormat(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<instrumentationManifest 
  xmlns=""http://schemas.microsoft.com/win/2004/08/events""
  xmlns:win=""http://manifests.microsoft.com/win/2004/08/windows/events""
  xmlns:xs=""http://www.w3.org/2001/XMLSchema""
  >
  <instrumentation>
    <events>
      <provider
        name=""{0}""
        symbol=""{1}""
        guid=""{2}""
        resourceFileName=""placeholder.dll""
        messageFileName=""placeholder.dll""
        >
        <channels>{6}
        </channels>
        <tasks>{3}
        </tasks>
        <templates>{4}
        </templates>
        <events>{5}
        </events>
      </provider>
    </events>
  </instrumentation>
  <localization>
    <resources culture=""en-US"">
      <stringTable>{7}
      </stringTable>
    </resources>
  </localization>
</instrumentationManifest>",
            input.Name,
            m_safeProviderName,
            input.Guid,
            tasks,
            templates,
            events,
            channels,
            stringTable
            );

            var xmlManifest = manifest.ToString();

            File.WriteAllText(OutputDir + m_filename + ".man", xmlManifest, Encoding.UTF8);

            return xmlManifest;
        }

        private static string GetSafeString(string name)
        {
            return name.Replace('-', '_').Replace('.', '_');
        }

        private static Guid GetGuidFromName(string name)
        {
            name = name.ToUpperInvariant();     // names are case insensitive.

            // The algorithm below is following the guidance of http://www.ietf.org/rfc/rfc4122.txt
            // Create a blob containing a 16 byte number representing the namespace
            // followed by the Unicode bytes in the name.

            var bytes = new byte[name.Length * 2 + 16];
            uint namespace1 = 0x482C2DB2;
            uint namespace2 = 0xC39047c8;
            uint namespace3 = 0x87F81A15;
            uint namespace4 = 0xBFC130FB;

            // Write the bytes most-significant byte first.  
            for (int i = 3; 0 <= i; --i)
            {
                bytes[i] = (byte)namespace1;
                namespace1 >>= 8;
                bytes[i + 4] = (byte)namespace2;
                namespace2 >>= 8;
                bytes[i + 8] = (byte)namespace3;
                namespace3 >>= 8;
                bytes[i + 12] = (byte)namespace4;
                namespace4 >>= 8;
            }

            // Write out  the name, most significant byte first
            for (int i = 0; i < name.Length; i++)
            {
                bytes[2 * i + 16 + 1] = (byte)name[i];
                bytes[2 * i + 16] = (byte)(name[i] >> 8);
            }

            // Compute the Sha1 hash 
            var sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(bytes);

            // Create a GUID out of the first 16 bytes of the hash (SHA-1 create a 20 byte hash)
            int a = (((((hash[3] << 8) + hash[2]) << 8) + hash[1]) << 8) + hash[0];
            short b = (short)((hash[5] << 8) + hash[4]);
            short c = (short)((hash[7] << 8) + hash[6]);

            c = (short)((c & 0x0FFF) | 0x5000);   // Set high 4 bits of octet 7 to 5, as per RFC 4122

            Guid guid = new Guid(a, b, c, hash[8], hash[9], hash[10], hash[11], hash[12], hash[13], hash[14], hash[15]);

            return guid;
        }

        private static string HandleEventArgs(StringBuilder templates, int eventId, object[] args)
        {
            if ((args == null) || (args.Length == 0))
            {
                return null;
            }

            var templateId = "template" + eventId;

            var template = new StringBuilder();
            template.AppendFormat(@"
          <template tid=""{0}"">",
                templateId
                );

            foreach (var arg in args)
            {
                var a = arg as Arg;
                var va = arg as VarArgs;
                if (a != null)
                {
                    var type = a.Type == Type.Guid ? "GUID" : a.Type.ToString();
                    template.AppendFormat(@"
            <data name=""{0}"" inType=""win:{1}"" />",
                        a.Name,
                        type
                        );
                }
                else if (va != null)
                {
                    template.AppendFormat(@"
            <data name=""{0}"" inType=""win:{1}"" />",
                        va.Name,
                        va.Type
                        );
                }
                else
                {
                    throw new InvalidDataException(System.String.Format("Invalid argument type: {0}", arg.GetType()));
                }
            }

            template.Append(@"
          </template>"
                );
            templates.Append(template.ToString());

            return templateId;
        }

        private void GenerateHeader(EventProvider input, string xmlManifest)
        {
            var eventMethods = new StringBuilder();

            foreach (var item in input.Items)
            {
                var e = item as EventProviderEvent;
                var t = item as EventProviderTask;
                var c = item as EventProviderChannel;
                if (e != null)
                {
                    HandleEventMethods(eventMethods, e.Name, m_safeProviderName + "_" + e.Name, e.Items);
                }
                else if (t != null)
                {
                    HandleEventMethods(eventMethods, t.Name + "Start", m_safeProviderName + "_" + t.Name + "_Start", t.Start);
                    HandleEventMethods(eventMethods, t.Name + "Stop", m_safeProviderName + "_" + t.Name + "_Stop", t.Stop);
                }
                else if (c != null)
                {
                    // noop
                }
                else
                {
                    throw new InvalidDataException(System.String.Format("Invalid event type: {0}", item.GetType()));
                }
            }

            var manifest = new StringBuilder();
            manifest.AppendFormat(
@"#pragma once

struct _EVENT_FILTER_DESCRIPTOR;

void EventCallback{1}(
    _In_ const GUID* SourceId,
    _In_ ULONG ControlCode,
    _In_ UCHAR Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ _EVENT_FILTER_DESCRIPTOR* FilterData,
    _Inout_opt_ PVOID CallbackContext
    );

#define MCGEN_PRIVATE_ENABLE_CALLBACK_V2 EventCallback{1}

#include ""{0}Base.h""

class __declspec(uuid(""{3}"")) {0}Base
{{
public:

    {0}Base()
        : m_delayedEventWrite(false)
    {{
        EventRegister{1}();

        if (m_delayedEventWrite)
        {{
            EventWriteManifest();
        }}
    }}

    ~{0}Base()
    {{
        if ({1}Handle != 0)
        {{
            EventWriteManifest();
        }}
        EventUnregister{1}();
    }}

    bool IsEnabled()
    {{
        return {1}_Context.IsEnabled == EVENT_CONTROL_CODE_ENABLE_PROVIDER;
    }}
{2}
    void EventDelayedWriteManifest()
    {{
        m_delayedEventWrite = true;
    }}

    void EventWriteManifest()
    {{
        static const char manifest[] =
            R""manifest({4})manifest"";

        // Currently a single chunk supported
        static_assert(sizeof(manifest) < ManifestEnvelope::MaxChunkSize, ""Only one manifest chunk currently supported"");

        EVENT_DESCRIPTOR eventDescr = {{ 0xFFFE, 1, 0, 0, 0xFE, 0xFFFE, -1 }};
        ManifestEnvelope envelope = {{ 1, 1, 0, 0x5B, 1, 0 }};

        EVENT_DATA_DESCRIPTOR dataDescr[2] = {{}};
        dataDescr[0].Ptr = reinterpret_cast<ULONGLONG>(&envelope);
        dataDescr[0].Size = sizeof(envelope);
        dataDescr[1].Ptr = reinterpret_cast<ULONGLONG>(manifest);
        dataDescr[1].Size = sizeof(manifest) - 1;

        ULONG ret = EventWrite({1}Handle, &eventDescr, ARRAYSIZE(dataDescr), dataDescr);
#ifndef NDEBUG
        if (ret != ERROR_SUCCESS)
        {{
            __debugbreak();
        }}
#endif
    }}

private:

    #pragma pack(push, 1)
    struct ManifestEnvelope
    {{
        uint8_t Format;
        uint8_t MajorVersion;
        uint8_t MinorVersion;
        uint8_t Magic;
        uint16_t TotalChunks;
        uint16_t ChunkNumber;

        static const uint16_t MaxChunkSize = 0xFF00;
    }};
    #pragma pack(pop)

    bool m_delayedEventWrite;
}};

__declspec(selectany) {0}Base {0};

inline void EventCallback{1}(
    _In_ const GUID* /*SourceId*/,
    _In_ ULONG ControlCode,
    _In_ UCHAR /*Level*/,
    _In_ ULONGLONG /*MatchAnyKeyword*/,
    _In_ ULONGLONG /*MatchAllKeyword*/,
    _In_opt_ _EVENT_FILTER_DESCRIPTOR* /*FilterData*/,
    _Inout_opt_ PVOID /*CallbackContext*/
    )
{{
    if (ControlCode == EVENT_CONTROL_CODE_ENABLE_PROVIDER)
    {{
        // The callback may be called during the call to EventRegisterXxx(), in which
        // case the handle is not set yet. Manifest will be sent at the end of the
        // constructor after EventRegister{1}() completes
        if ({1}Handle != 0)
        {{
            {0}.EventWriteManifest();
        }}
        else
        {{
            {0}.EventDelayedWriteManifest();
        }}
    }}

    if (ControlCode == EVENT_CONTROL_CODE_DISABLE_PROVIDER)
    {{
        {0}.EventWriteManifest();
    }}
}}",
            m_filename,         // {0} ex: 'EtwLogger'
            m_safeProviderName, // {1} ex: 'MMaitre_TraceEtw'
            eventMethods,       // {2}
            input.Guid,         // {3} ex: '{d7bcc40a-0866-52c3-aade-b3c8d32fd38e}'
            xmlManifest         // {4}
            );

            File.WriteAllText(OutputDir + m_filename + ".h", manifest.ToString(), Encoding.UTF8);
        }

        private static void HandleEventMethods(StringBuilder eventMethods, string name, string symbol, object[] args)
        {
            var argAndTypeList = new StringBuilder();
            var argList = new StringBuilder();

            if ((args != null) && (args.Length > 0))
            {
                var va = args[0] as VarArgs;
                if ((args.Length == 1) && (va != null))
                {
                    string type;
                    string printf;
                    switch (va.Type)
                    {
                        case String.AnsiString: 
                            type = "char";
                            printf = "vsprintf_s";
                            break;
                        case String.UnicodeString: 
                            type = "wchar_t";
                            printf = "vswprintf_s";
                            break;
                        default: throw new ArgumentException();
                    }

                    eventMethods.AppendFormat(@"
    void {0}(_In_z_ const {1}* format, ...)
    {{
        if (!EventEnabled{3}())
        {{
            return;
        }}

        {1} message[1024];

        va_list args;
        va_start(args, format);
        {4}(message, format, args);
        va_end(args);

        EventWrite{3}(message);
    }}
",
                    name,
                    type,
                    GetCamelCasedString(va.Name),
                    symbol,
                    printf
                        );
                }
                else
                {
                    bool addComma = false;
                    foreach (var arg in args)
                    {
                        if (addComma)
                        {
                            argAndTypeList.Append(", ");
                            argList.Append(", ");
                        }

                        var a = arg as Arg;
                        if (a != null)
                        {
                            argAndTypeList.AppendFormat("{0} {1}", GetCppType(a.Type), GetCamelCasedString(a.Name));
                            argList.AppendFormat("{0}", GetCamelCasedString(a.Name));
                            addComma = true;
                        }
                        else
                        {
                            throw new InvalidDataException(System.String.Format("Invalid argument type: {0}", arg.GetType()));
                        }
                    }

                    eventMethods.AppendFormat(@"
    void {0}({1})
    {{
        EventWrite{3}({2});
    }}
",
                        name,
                        argAndTypeList,
                        argList,
                        symbol
                    );
                }
            }
            else
            {
                eventMethods.AppendFormat(@"
    void {0}()
    {{
        EventWrite{1}();
    }}
",
                    name,
                    symbol
                );
            }
        }

        private static string GetCppType(Type type)
        {
            switch (type)
            {
                case Type.Boolean: return "_In_ const BOOL";
                case Type.Int8: return "_In_ const char";
                case Type.UInt8: return "_In_ const UCHAR";
                case Type.Int16: return "_In_ const signed short";
                case Type.UInt16: return "_In_ const unsigned short";
                case Type.Int32: return "_In_ const signed int";
                case Type.UInt32: return "_In_ const unsigned int";
                case Type.Int64: return "_In_ signed __int64";
                case Type.UInt64: return "_In_ unsigned __int64";
                case Type.Float: return "_In_ const float";
                case Type.Double: return "_In_ const double";
                case Type.Guid: return "_In_ LPCGUID";
                case Type.Pointer: return "_In_opt_ const void *";
                case Type.AnsiString: return "_In_opt_ LPCSTR";
                case Type.UnicodeString: return "_In_opt_ PCWSTR";
                default: throw new ArgumentException();
            }
        }

        private static string GetCppType(String type)
        {
            switch (type)
            {
                case String.AnsiString: return "_In_opt_ LPCSTR";
                case String.UnicodeString: return "_In_opt_ PCWSTR";
                default: throw new ArgumentException();
            }
        }

        private static string GetCamelCasedString(string name)
        {
            return name[0].ToString().ToLower() + name.Substring(1);
        }

    }
}
