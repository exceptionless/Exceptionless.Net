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
         Assert.Equal("mp3", "MP3".ToLowerUnderscoredWords());
         Assert.Equal("flac", "FLAC".ToLowerUnderscoredWords());
         Assert.Equal("ip_address", "IP Address".ToLowerUnderscoredWords());
         Assert.Equal("ip_address", "IPAddress".ToLowerUnderscoredWords());
      }
   }
}
