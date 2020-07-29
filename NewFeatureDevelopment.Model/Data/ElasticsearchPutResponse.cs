using System.Diagnostics.CodeAnalysis;
using static NFD.Domain.Data.ElasticsearchQueryResult;

namespace NFD.Domain.Data
{
    [ExcludeFromCodeCoverage]
    public class ElasticsearchPutResponse
    {
        public string _index { get; set; }
        public string _type { get; set; }
        public string _id { get; set; }
        public int _version { get; set; }
        public string result { get; set; }
        public _Shards _shards { get; set; }
        public int _seq_no { get; set; }
        public int _primary_term { get; set; }
    }
}
