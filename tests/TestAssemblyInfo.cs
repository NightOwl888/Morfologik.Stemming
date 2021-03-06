﻿using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

// TODO: Some tests are not always completing, so adding this
// attribute to explictly fail so the mystery test can be investigated.
[assembly: Timeout(60000)]

[assembly: SuppressMessage("Microsoft.Design", "CA1034", Justification = "We don't care about Java-style classes in tests")]
[assembly: SuppressMessage("Microsoft.Design", "CA1031", Justification = "We don't care about Java-style classes in tests")]
[assembly: SuppressMessage("Microsoft.Design", "CA1822", Justification = "We don't care about Java-style classes in tests")]
