using Exceptionless.Models;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class SettingsDictionaryFormatter : DictionaryFormatterBase<string, string, SettingsDictionary> {
        protected override SettingsDictionary Create(int count) {
            return new SettingsDictionary();
        }

        protected override void Add(SettingsDictionary collection, int index, string key, string value) {
            collection.Add(key, value);
        }
    }
}
