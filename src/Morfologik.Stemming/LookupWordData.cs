using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morfologik.Stemming
{
    internal sealed class LookupWordData : WordData2
    {
        private readonly DictionaryLookupResult result;
        private int index;

        internal LookupWordData(DictionaryLookupResult result)
        {
            this.result = result;
        }

        internal void SetIndex(int index)
        {
            this.index = index;
        }

        public override ReadOnlyMemory<char> Word => result.Word;

        public override ReadOnlyMemory<char> Stem => result.GetStem(index);

        public override ReadOnlyMemory<char> Tag => result.GetTag(index);
    }
}
