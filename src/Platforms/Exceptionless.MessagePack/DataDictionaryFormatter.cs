using System.Collections.Generic;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using MessagePack;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class DataDictionaryFormatter : IMessagePackFormatter<DataDictionary> {
        public int Serialize(ref byte[] bytes, int offset, DataDictionary value, IFormatterResolver formatterResolver) {
            if (value == null) {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            var startOffset = offset;
            offset += MessagePackBinary.WriteMapHeader(ref bytes, offset, value.Count);
            foreach (var item in value) {
                offset += MessagePackBinary.WriteString(ref bytes, offset, item.Key);
                switch (item.Key) {
                    case Event.KnownDataKeys.EnvironmentInfo:
                        offset += formatterResolver.GetFormatter<EnvironmentInfo>()
                            .Serialize(ref bytes, offset, (EnvironmentInfo)item.Value, formatterResolver);
                        break;
                    case Event.KnownDataKeys.Error:
                        offset += formatterResolver.GetFormatter<Error>()
                            .Serialize(ref bytes, offset, (Error)item.Value, formatterResolver);
                        break;
                    case Event.KnownDataKeys.Level:
                        offset += MessagePackBinary.WriteString(ref bytes, offset, (string)item.Value);
                        break;
                    case Event.KnownDataKeys.ManualStackingInfo:
                        offset += formatterResolver.GetFormatter<ManualStackingInfo>()
                            .Serialize(ref bytes, offset, (ManualStackingInfo)item.Value, formatterResolver);
                        break;
                    case Event.KnownDataKeys.RequestInfo:
                        offset += formatterResolver.GetFormatter<RequestInfo>()
                            .Serialize(ref bytes, offset, (RequestInfo)item.Value, formatterResolver);
                        break;
                    case Event.KnownDataKeys.SimpleError:
                        offset += formatterResolver.GetFormatter<SimpleError>()
                            .Serialize(ref bytes, offset, (SimpleError)item.Value, formatterResolver);
                        break;
                    case Event.KnownDataKeys.SubmissionMethod:
                        offset += MessagePackBinary.WriteString(ref bytes, offset, (string)item.Value);
                        break;
                    case Event.KnownDataKeys.TraceLog:
                        offset += formatterResolver.GetFormatter<List<string>>()
                            .Serialize(ref bytes, offset, (List<string>)item.Value, formatterResolver);
                        break;
                    case Event.KnownDataKeys.UserDescription:
                        offset += formatterResolver.GetFormatter<UserDescription>()
                            .Serialize(ref bytes, offset, (UserDescription)item.Value, formatterResolver);
                        break;
                    case Event.KnownDataKeys.UserInfo:
                        offset += formatterResolver.GetFormatter<UserInfo>()
                            .Serialize(ref bytes, offset, (UserInfo)item.Value, formatterResolver);
                        break;
                    case Event.KnownDataKeys.Version:
                        offset += MessagePackBinary.WriteString(ref bytes, offset, (string)item.Value);
                        break;
                    default:
                        offset += formatterResolver.GetFormatter<object>()
                            .Serialize(ref bytes, offset, item.Value, formatterResolver);
                        break;
                }
            }

            return offset - startOffset;
        }

        public DataDictionary Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize) {
            if (MessagePackBinary.IsNil(bytes, offset)) {
                readSize = 1;
                return null;
            }
            var startOffset = offset;
            var len = MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;
            var dic = new DataDictionary();
            for (int i = 0; i < len; i++) {
                var key = MessagePackBinary.ReadString(bytes, offset, out readSize);
                offset += readSize;
                switch (key) {
                    case Event.KnownDataKeys.EnvironmentInfo: {
                            var value = formatterResolver.GetFormatter<EnvironmentInfo>().Deserialize(bytes, offset, formatterResolver, out readSize);
                            offset += readSize;
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.Error: {
                            var value = formatterResolver.GetFormatter<Error>().Deserialize(bytes, offset, formatterResolver, out readSize);
                            offset += readSize;
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.Level: {
                            var value = MessagePackBinary.ReadString(bytes, offset, out readSize);
                            offset += readSize;
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.ManualStackingInfo: {
                            var value = formatterResolver.GetFormatter<ManualStackingInfo>().Deserialize(bytes, offset, formatterResolver, out readSize);
                            offset += readSize;
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.RequestInfo: {
                            var value = formatterResolver.GetFormatter<RequestInfo>().Deserialize(bytes, offset, formatterResolver, out readSize);
                            offset += readSize;
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.SimpleError: {
                            var value = formatterResolver.GetFormatter<SimpleError>().Deserialize(bytes, offset, formatterResolver, out readSize);
                            offset += readSize;
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.SubmissionMethod: {
                            var value = MessagePackBinary.ReadString(bytes, offset, out readSize);
                            offset += readSize;
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.TraceLog: {
                            var value = formatterResolver.GetFormatter<List<string>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                            offset += readSize;
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.UserDescription: {
                            var value = formatterResolver.GetFormatter<UserDescription>().Deserialize(bytes, offset, formatterResolver, out readSize);
                            offset += readSize;
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.UserInfo: {
                            var value = formatterResolver.GetFormatter<UserInfo>().Deserialize(bytes, offset, formatterResolver, out readSize);
                            offset += readSize;
                            dic.Add(key, value);
                            break;
                        }
                    case Event.KnownDataKeys.Version: {
                            var value = MessagePackBinary.ReadString(bytes, offset, out readSize);
                            offset += readSize;
                            dic.Add(key, value);
                            break;
                        }
                    default: {
                            var value = formatterResolver.GetFormatter<object>().Deserialize(bytes, offset, formatterResolver, out readSize);
                            offset += readSize;
                            dic.Add(key, value);
                            break;
                        }
                }
            }
            readSize = offset - startOffset;
            return dic;
        }
    }
}
