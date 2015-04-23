using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exceptionless.Extras;
using Exceptionless.Logging;
using Xunit;

namespace Exceptionless.Tests.ToErrorModel
{
    public class ToErrorModelTests
    {
        [Fact]
        public void SimpleExceptionHasNoInner() {
            var e = new Exception("Test exception");
            var model = e.ToErrorModel(GetLog());

            Assert.Equal(e.Message, model.Message);
            Assert.Null(model.Inner);
        }

        [Fact]
        public void SimpleNestedExceptionWithInner()
        {
            var e = new Exception("Outer", new Exception("Inner"));
            var model = e.ToErrorModel(GetLog());

            Assert.Equal("Outer", model.Message);
            Assert.Equal("Inner", model.Inner.Message);
        }

        [Fact]
        public void AggregateExceptionWithOneInner() {
            var e = new AggregateException(new Exception("Exception 1"));

            var model = e.ToErrorModel(GetLog());

            Assert.Equal("Exception 1", model.Inner.Message);
        }

        [Fact]
        public void AggregateExceptionWithMultipleInners() {
            var e = new AggregateException(new Exception("Exception 1"), new Exception("Exception 2"));

            var model = e.ToErrorModel(GetLog());

            Assert.Equal("Exception 1", model.Inner.Message);

            // no way to get at "Exception 2"
        }

        [Fact]
        public void AggregateExceptionWithInnerAggregateException() {
            var e = new AggregateException(new AggregateException(new Exception("Exception 1.1"), new Exception("Exception 1.2")), new Exception("Exception 2"));

            var model = e.ToErrorModel(GetLog());

            // now there's no way to find 1.1, 1.2 or 2 (Needs a .Flatten())
            Assert.Equal("Exception 1.1", model.Inner.Message);
        }


        IExceptionlessLog GetLog() {
            return new NullExceptionlessLog();
        }
    }
}
