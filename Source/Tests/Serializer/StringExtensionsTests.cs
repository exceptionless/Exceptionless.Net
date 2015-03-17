using Exceptionless.Extensions;
using Xunit;

namespace Exceptionless.Tests.Serializer
{
   public class StringExtensionsTests
   {
      [Fact]
      public void LowerUnderscoredWords()
      {
         Assert.Equal("blake_niemyjski_1", "blakeNiemyjski 1".ToLowerUnderscoredWords());
         Assert.Equal("blake_niemyjski_2", "Blake     Niemyjski 2".ToLowerUnderscoredWords());
         Assert.Equal("blake_niemyjski_3", "Blake_ niemyjski 3".ToLowerUnderscoredWords());
         Assert.Equal("blake_niemyjski4", "Blake_Niemyjski4".ToLowerUnderscoredWords());
         Assert.Equal("mp3_files_data", "MP3FilesData".ToLowerUnderscoredWords());
         Assert.Equal("flac", "FLAC".ToLowerUnderscoredWords());
         Assert.Equal("number_of_abcd_things", "NumberOfABCDThings".ToLowerUnderscoredWords());
         Assert.Equal("ip_address_2s", "IPAddress 2s".ToLowerUnderscoredWords());
         Assert.Equal("127.0.0.1", "127.0.0.1".ToLowerUnderscoredWords());
      }
   }
}
