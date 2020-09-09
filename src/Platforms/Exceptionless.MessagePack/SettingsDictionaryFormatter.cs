using Exceptionless.Models;
using MessagePack;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class SettingsDictionaryFormatter : DictionaryFormatterBase<string, string, SettingsDictionary> {
        protected override SettingsDictionary Create(int count, MessagePackSerializerOptions options) {
            return new SettingsDictionary();
        }

        protected override void Add(SettingsDictionary collection, int index, string key, string value, MessagePackSerializerOptions options) {
            collection.Add(key, value);
        }
    }
}
