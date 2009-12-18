using Newtonsoft.Json;

namespace RedBranch.Hammock
{
    public class Document
    {
        [JsonIgnore] public Session Session { get; set; }
        [JsonIgnore] public string Id { get; set; }
        [JsonIgnore] public string Revision { get; set; }

        [JsonIgnore] public string Location
        {
            get
            {
                return Session.Connection.GetDatabaseLocation(Session.Database) +
                       (Id.StartsWith("_design/")
                            ? "_design/" + Id.Substring(8).Replace("/", "%2F")
                            : Id.Replace("/", "%2F"));
            }
        }

        public override int GetHashCode()
        {
            return (Id ?? "/").GetHashCode() ^ (Revision ?? "-").GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var d = obj as Document;
            return null == d
                       ? base.Equals(obj)
                       : d.Id == Id &&
                         d.Revision == Revision;
        }

        public static string For<TEntity>(string withId)
        {
            return string.Format("{0}-{1}", typeof (TEntity).Name.ToLowerInvariant(), withId);
        }
    }

    public interface IHasDocument
    {
        [JsonIgnore]
        Document Document { get; set; }
    }
}