using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public static class Base
    {
        public const string HTTP_NODNS = @"HTTP/1.1 502 Bad Gateway
Content-Type: text/plain
Content-Length: 90

The address was unreachable. Please check if it is valid and present in your address book.";
    }
}
