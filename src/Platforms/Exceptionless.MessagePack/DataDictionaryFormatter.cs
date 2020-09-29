using System.Collections.Generic;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using MessagePack;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class DataDictionaryFormatter : IMessagePackFormatter<DataDictionary> {
        public void Serialize(ref MessagePackWriter writer, DataDictionary value, MessagePackSerializerOptions options) {
            if (value == null) {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(value.Count);
            foreach (var item in value) {
                writer.Write(item.Key);
                switch (item.Key) {
                    case Event.KnownDataKeys.EnvironmentInfo:
                        options.Resolver.GetFormatter<EnvironmentInfo>()
                            .Serialize(ref writer, (EnvironmentInfo)item.Value, options);
                        break;
                    case Event.KnownDataKeys.Error:
                        options.Resolver.GetFormatter<Error>()
                            .Serialize(ref writer, (Error)item.Value, options);
                        break;
                    case Event.KnownDataKeys.Level:
                        writer.Write((string)item.Value);
                        break;
                    case Event.KnownDataKeys.ManualStackingInfo:
                        options.Resolver.GetFormatter<ManualStackingInfo>()
                            .Serialize(ref writer, (ManualStackingInfo)item.Value, options);
                        break;
                    case Event.KnownDataKeys.RequestInfo:
                        options.Resolver.GetFormatter<RequestInfo>()
                            .Serialize(ref writer, (RequestInfo)item.Value, options);
                        break;
                    case Event.KnownDataKeys.SimpleError:
                        options.Resolver.GetFormatter<SimpleError>()
                            .Serialize(ref writer, (SimpleError)item.Value, options);
                        break;
                    case Event.KnownDataKeys.SubmissionMethod:
                        writer.Write((string)item.Value);
                        break;
                    case Event.KnownDataKeys.TraceLog:
                        options.Resolver.GetFormatter<List<string>>()
                            .Serialize(ref writer, (List<string>)item.Value, options);
                        break;
                    case Event.KnownDataKeys.UserDescription:
                        options.Resolver.GetFormatter<UserDescription>()
                            .Serialize(ref writer, (UserDescription)item.Value, options);
                        break;
                    case Event.KnownDataKeys.UserInfo:
                        options.Resolver.GetFormatter<UserInfo>()
                            .Serialize(ref writer, (UserInfo)item.Value, options);
                        break;
                    case Event.KnownDataKeys.Version:
                        writer.Write((string)item.Value);
                        break;
                    default:
                        options.Resolver.GetFormatter<object>()
                            .Serialize(ref writer, item.Value, options);
                        break;
                }
            }
        }

        public DataDictionary Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
            if (reader.IsNil)
                return null;
            
            var len = reader.ReadMapHeader();
            var dic = new DataDictionary();
            for (int i = 0; i < len; i++) {
                var key = reader.ReadString();
                switch (key) {
                    case Event.KnownDataKeys.EnvironmentInfo: {
                            var value = options.Resolver.GetFormatter<EnvironmentInfo>().Deserialize(ref reader, options);
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.Error: {
                            var value = options.Resolver.GetFormatter<Error>().Deserialize(ref reader, options);
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.Level: {
                            var value = reader.ReadString();
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.ManualStackingInfo: {
                            var value = options.Resolver.GetFormatter<ManualStackingInfo>().Deserialize(ref reader, options);
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.RequestInfo: {
                            var value = options.Resolver.GetFormatter<RequestInfo>().Deserialize(ref reader, options);
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.SimpleError: {
                            var value = options.Resolver.GetFormatter<SimpleError>().Deserialize(ref reader, options);
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.SubmissionMethod: {
                            var value = reader.ReadString();
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.TraceLog: {
                            var value = options.Resolver.GetFormatter<List<string>>().Deserialize(ref reader, options);
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.UserDescription: {
                            var value = options.Resolver.GetFormatter<UserDescription>().Deserialize(ref reader, options);
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.UserInfo: {
                            var value = options.Resolver.GetFormatter<UserInfo>().Deserialize(ref reader, options);
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.Version: {
                            var value = reader.ReadString();
                            dic.Add(key, value);
                            break;
                        }
#if NETSTANDARD
                    case "ProcessArchitecture": {
                        var value = reader.ReadInt32();
                        dic.Add(key, (System.Runtime.InteropServices.Architecture)value);
                        break;
                    }
#endif
                    default: {
                            var value = options.Resolver.GetFormatter<object>().Deserialize(ref reader, options);
                            dic.Add(key, value);
                            break;
                        }
                }
            }
            
            return dic;
        }
    }
}
