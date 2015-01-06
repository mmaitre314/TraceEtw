using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            GenerateManifest(input);

            Log.LogMessage(MessageImportance.Normal, "Generating base header");
            if (!GenerateBaseHeader())
            {
                return false;
            }

            Log.LogMessage(MessageImportance.Normal, "Generating header");
            GenerateHeader(input);

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
            return input;
        }

        private void GenerateManifest(EventProvider input)
        {
            var events = new StringBuilder();
            var tasks = new StringBuilder();
            var templates = new StringBuilder();

            int eventId = 1;
            int taskId = 1;

            foreach (var item in input.Items)
            {
                var e = item as EventProviderEvent;
                var t = item as EventProviderTask;
                if (e != null)
                {
                    var templateId = HandleEventArgs(templates, eventId, e.Items);

                    events.AppendFormat(@"
          <event value=""{0}"" symbol=""{4}_{1}"" task=""{1}"" opcode=""win:Info"" level=""win:{2}"" {3}/>",
                        eventId,
                        e.Name,
                        e.Level,
                        templateId == null ? "" : @"template=""" + templateId + @""" ",
                        m_safeProviderName
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
                else
                {
                    throw new InvalidDataException(System.String.Format("Invalid event type: {0}", item.GetType()));
                }
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
        <tasks>{3}
        </tasks>
        <templates>{4}
        </templates>
        <events>{5}
        </events>
      </provider>
    </events>
  </instrumentation>
</instrumentationManifest>
",
            input.Name,
            m_safeProviderName,
            input.Guid,
            tasks,
            templates,
            events
            );

            File.WriteAllText(OutputDir + m_filename + ".man", manifest.ToString(), Encoding.UTF8);
        }

        private string GetSafeString(string name)
        {
            return name.Replace('-', '_').Replace('.', '_');
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

        private void GenerateHeader(EventProvider input)
        {
            var eventMethods = new StringBuilder();

            foreach (var item in input.Items)
            {
                var e = item as EventProviderEvent;
                var t = item as EventProviderTask;
                if (e != null)
                {
                    HandleEventMethods(eventMethods, e.Name, m_safeProviderName + "_" + e.Name, e.Items);
                }
                else if (t != null)
                {
                    HandleEventMethods(eventMethods, t.Name + "Start", m_safeProviderName + "_" + t.Name + "_Start", t.Start);
                    HandleEventMethods(eventMethods, t.Name + "Stop", m_safeProviderName + "_" + t.Name + "_Stop", t.Stop);
                }
                else
                {
                    throw new InvalidDataException(System.String.Format("Invalid event type: {0}", item.GetType()));
                }
            }

            var manifest = new StringBuilder();
            manifest.AppendFormat(
@"#pragma once

#include ""{0}Base.h""

class {0}
{{
public:

    static bool IsEnabled()
    {{
        return {1}_Context.IsEnabled == EVENT_CONTROL_CODE_ENABLE_PROVIDER;
    }}
{2}
private:

    static class EventProviderLifetime
    {{
    public:

        EventProviderLifetime()
        {{
            EventRegister{1}();
        }}

        ~EventProviderLifetime()
        {{
            EventUnregister{1}();
        }}

    }} s_lifetime;
}};

__declspec(selectany) {0}::EventProviderLifetime {0}::s_lifetime;
",
            m_filename,
            m_safeProviderName,
            eventMethods
            );

            File.WriteAllText(OutputDir + m_filename + ".h", manifest.ToString(), Encoding.UTF8);
        }

        private static void HandleEventMethods(StringBuilder eventMethods, string name, string symbol, object[] args)
        {
            var argAndTypeList = new StringBuilder();
            var argList = new StringBuilder();

            if ((args != null) && (args.Length > 0))
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
                    var va = arg as VarArgs;
                    if (a != null)
                    {
                        argAndTypeList.AppendFormat("{0} {1}", GetCppType(a.Type), GetCamelCasedString(a.Name));
                        argList.AppendFormat("{0}", GetCamelCasedString(a.Name));
                        addComma = true;
                    }
                    else if (va != null)
                    {
                        argAndTypeList.AppendFormat("{0} {1}", GetCppType(va.Type), GetCamelCasedString(va.Name));
                        argList.AppendFormat("{0}", GetCamelCasedString(va.Name));
                        addComma = true;
                    }
                    else
                    {
                        throw new InvalidDataException(System.String.Format("Invalid argument type: {0}", arg.GetType()));
                    }
                }
            }

            eventMethods.AppendFormat(@"
    static void {0}({1})
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
