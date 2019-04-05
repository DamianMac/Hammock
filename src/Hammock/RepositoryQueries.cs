// 
//  RepositoryQueries.cs
//  
//  Author:
//       Nick Nystrom <nnystrom@gmail.com>
//  
//  Copyright (c) 2009-2011 Nicholas J. Nystrom
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Newtonsoft.Json.Linq;
using RedBranch.Hammock.Design;

namespace RedBranch.Hammock
{
    #region Adhoc Query Interfaces

    public interface IPrimayOperator<TEntity, TKey> : ISecondaryOperator<TEntity, TKey> where TEntity : class
    {
        IPrimaryExpression<TEntity> Eq(TKey value);
    }

    public interface IPrimaryExpression<TEntity> : ISecondaryExpression<TEntity> where TEntity : class
    {
        new IPrimayOperator<TEntity, TKey> And<TKey>(Expression<Func<TEntity, TKey>> xp);
    }

    public interface ISecondaryOperator<TEntity, TKey> : ITertiaryOperator<TEntity, TKey> where TEntity : class
    {
        ISecondaryExpression<TEntity> Bw(TKey lower, TKey upper);
        ISecondaryExpression<TEntity> Ge(TKey value);
    }

    public interface ISecondaryExpression<TEntity> : ITertiaryExpression<TEntity> where TEntity : class
    {
        new ISecondaryOperator<TEntity, TKey> And<TKey>(Expression<Func<TEntity, TKey>> xp);
    }

    public interface ITertiaryOperator<TEntity, TKey> where TEntity : class
    {
        ITertiaryExpression<TEntity> Le(TKey value);
        ITertiaryExpression<TEntity> Like(string value);
    }

    public interface ITertiaryExpression<TEntity> : IEnumerable<TEntity> where TEntity : class
    {
        ITertiaryOperator<TEntity, TKey> And<TKey>(Expression<Func<TEntity, TKey>> xp);
        ITertiaryExpression<TEntity> Returns<TKey2>(Expression<Func<TEntity, TKey2>> xp);

        Query<TEntity>.Spec Spec();
        Query<TEntity>.Result List();
        TEntity Single();
        TEntity SingleOrDefault();
    }

    #endregion

    public partial class Repository<TEntity>
    {
        #region Adhoc Query Implmentations

        private class PrimaryExpression<TKey> : SecondaryExpression<TKey>, IPrimayOperator<TEntity, TKey>, IPrimaryExpression<TEntity>
        {
            public PrimaryExpression(ExpressionValues values) : base(values)
            { 
            }

            public IPrimaryExpression<TEntity> Eq(TKey value)
            {
                (Values.Startkey ?? (Values.Startkey = new JArray())).Add(value);
                (Values.Endkey ?? (Values.Endkey = new JArray())).Add(value);
                return this;
            }

            public new IPrimayOperator<TEntity, TKey2> And<TKey2>(Expression<Func<TEntity, TKey2>> xp)
            {
                return new PrimaryExpression<TKey2>(Values.AppendExpression(xp.Body));
            }
        }

        private class SecondaryExpression<TKey> : TertiaryExpression<TKey>, ISecondaryOperator<TEntity, TKey>, ISecondaryExpression<TEntity>
        {
            protected SecondaryExpression(ExpressionValues values) : base(values)
            {
            }

            public ISecondaryExpression<TEntity> Bw(TKey lower, TKey upper)
            {
                (Values.Startkey ?? (Values.Startkey = new JArray())).Add(lower);
                (Values.Endkey ?? (Values.Endkey = new JArray())).Add(upper);
                return this;
            }   

            public ISecondaryExpression<TEntity> Ge(TKey value)
            {
                (Values.Startkey ?? (Values.Startkey = new JArray())).Add(value);
                (Values.Endkey ?? (Values.Endkey = new JArray())).Add(GetBounds(typeof(TKey)));
                return this;
            }

            public new ISecondaryOperator<TEntity, TKey2> And<TKey2>(Expression<Func<TEntity, TKey2>> xp)
            {
                return new SecondaryExpression<TKey2>(Values.AppendExpression(xp.Body));
            }
        }

        private class TertiaryExpression<TKey> : ITertiaryOperator<TEntity, TKey>, ITertiaryExpression<TEntity>
        {
            protected ExpressionValues Values { get; private set; }

            protected object GetBounds(Type t)
            {
                // weak, but json.net outputs scientific notation in 3.14159e+28 format, and 
                // couchdb dislikes the +, so we just use a Really Big Integer here instead :(
                switch (Type.GetTypeCode(t))
                {
                    case TypeCode.Boolean:  return true;
                    case TypeCode.Char:     return char.MaxValue;
                    case TypeCode.SByte:    
                    case TypeCode.Byte:     
                    case TypeCode.Int16:    
                    case TypeCode.UInt16:   
                    case TypeCode.Int32:    
                    case TypeCode.UInt32:   
                    case TypeCode.Int64:    
                    case TypeCode.UInt64:   
                    case TypeCode.Single:   
                    case TypeCode.Double:   
                    case TypeCode.Decimal:  return long.MaxValue;
                    case TypeCode.DateTime: return DateTime.MaxValue;
                    case TypeCode.String:   return "ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ";
                    default:
                        throw new ArgumentOutOfRangeException("Queryable fields must be one of the primitive types, not " + t.Name);
                }
            }

            protected TertiaryExpression(ExpressionValues values)
            {
                Values = values;
            }

            public ITertiaryExpression<TEntity> Le(TKey value)
            {
                (Values.Startkey ?? (Values.Startkey = new JArray())).Add(null);
                (Values.Endkey ?? (Values.Endkey = new JArray())).Add(value);
                return this;
            }

            public ITertiaryExpression<TEntity> Like(string value)
            {
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }
                if (Values.HasLike)
                {
                    throw new InvalidOperationException("'Like' can appear only once in a query.");
                }

                Values.HasLike = true;
                (Values.Startkey ?? (Values.Startkey = new JArray())).Add(value);
                (Values.Endkey ?? (Values.Endkey = new JArray())).Add(value + "Z");
                return this;
            }

            public ITertiaryOperator<TEntity, TKey2> And<TKey2>(Expression<Func<TEntity, TKey2>> xp)
            {
                return new TertiaryExpression<TKey2>(Values.AppendExpression(xp.Body));
            }

            public ITertiaryExpression<TEntity> Returns<TKey2>(Expression<Func<TEntity, TKey2>> xp)
            {
                Values.AppendReturns(xp.Body);
                return this;
            }

            public Query<TEntity>.Spec Spec()
            {
                return Values.CreateQuerySpec(Values.CreateQuery());
            }

            public Query<TEntity>.Result List()
            {
                return Spec().Execute();
            }

            public TEntity Single()
            {
                var x = SingleOrDefault();
                if (null == x)
                {
                    throw new Exception("No entity found.");
                }
                return x;
            }

            public TEntity SingleOrDefault()
            {
                var result = Spec().WithDocuments().Execute();
                return result.Rows.Length == 0 ? null : result.Rows.First().Entity;
            }

            public IEnumerator<TEntity> GetEnumerator()
            {
                return Spec().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ExpressionValues
        {
            public Repository<TEntity> Repository;
            public List<string> Fields;
            public List<string> Returns;
            public JArray Startkey;
            public JArray Endkey;

            public bool HasLike;

            public ExpressionValues AppendExpression(Expression x)
            {
                var js = BuildJavasriptExpressionFromLinq(x);
                if (Fields.Contains(js))
                {
                    throw new ArgumentException("Repository.Where() can accept a field only once. The field '" + x + "' appears more than once.");
                }
                Fields.Add(js);
                return this;
            }

            public ExpressionValues AppendReturns(Expression x)
            {
                var js = BuildJavasriptExpressionFromLinq(x);
                (Returns ?? (Returns = new List<string>())).Add(js);
                return this;
            }

            private View CreateView()
            {
                var a = new StringBuilder(Fields.Count*32);
                a.Append("function(doc) {");
                a.Append("\n if (doc._id.indexOf('");
                a.Append(typeof (TEntity).Name.ToLowerInvariant());
                a.Append("-') === 0) {");

                var likeField = Fields.Last();
                if (HasLike)
                {
                    a.AppendFormat("\n  if (doc{0}) {{", likeField);
                    a.AppendFormat("\n    for (var i=0; i<doc{0}.length; i++) {{", likeField);
                    a.AppendFormat("\n      if (doc{0}.length - i > 2) {{", likeField);
                }

                a.Append("\n  emit([");
                for (int n = 0; n < Fields.Count; n++)
                {
                    if (n > 0) a.Append(", ");
                    if (HasLike && n == Fields.Count-1)
                    {
                        a.AppendFormat("doc{0}.substr(i)", likeField);
                    }
                    else
                    {
                        a.Append("doc");
                        a.Append(Fields[n]);
                    }
                }
                a.Append("], ");

                if (null == Returns)
                {
                    a.Append("null");
                }
                else
                {
                    a.Append("[");
                    for (int n=0; n<Returns.Count; n++)
                    {
                        if (n > 0) a.Append(", ");
                        a.Append("doc");
                        a.Append(Returns[n]);
                    }
                    a.Append("]");
                }
                
                a.Append(");");
                
                if (HasLike)
                {
                    a.Append("\n      }");
                    a.Append("\n    }");
                    a.Append("\n  }");
                }
                
                a.Append("\n }");
                a.Append("\n}\n");

                return new View {Map = a.ToString()};
            }

            public Query<TEntity> CreateQuery()
            {
                // build a view name
                var a = new StringBuilder(16 * Fields.Count);
                a.Append("by");
                foreach (var f in Fields)
                {
                    a.Append('-');
                    a.Append(f.ToSlug());
                    if (HasLike)
                    {
                        a.Append("-with-like");
                    }
                }
                if (null != Returns)
                {
                    a.Append("-with-values");
                    foreach (var f in Returns)
                    {
                        a.Append(f.ToSlug());
                    }
                }
                var name = a.ToString();

                // add the view to the design doc if needed
                if (!Repository.Design.Views.ContainsKey(name))
                {
                    Repository.CreateView(name, CreateView());
                }

                return Repository.Queries[name];
            }

            public Query<TEntity>.Spec CreateQuerySpec(Query<TEntity> query)
            {
                var spec = new Query<TEntity>.Spec(query);
                if (null != Startkey) spec = spec.From(Startkey);
                if (null != Endkey) spec = spec.To(Endkey);
                return spec;
            }
        }

        #endregion

        private static string BuildJavasriptExpressionFromLinq(Expression x)
        {
            var js = "";

            while (null != x)
            {
                switch (x.NodeType)
                {
                    case ExpressionType.Parameter:
                        return js;

                    case ExpressionType.MemberAccess:

                        var xmember = (MemberExpression)x;
                        js = "." + xmember.Member.Name + js;
                        x = xmember.Expression;
                        break;

                    default:
                        throw new NotSupportedException("Repository.Where() currently only parses simply fields/property expressions. You tried to use a " + x.NodeType + " expression.");
                }
            }

            throw new Exception("Unexptected state encountered parsing Repository.Where() expression.");
        }

        protected Query<TEntity> WithView(
            string name, 
            string map)
        {
            return WithView(name, map, null);
        }

        protected Query<TEntity> WithView(
            string name, 
            string map,
            string reduce)
        {
            return WithView(name, new View {Map = map, Reduce = reduce});
        }

        protected Query<TEntity> WithView(
            string name, 
            View v)
        {
            if (!Design.Views.ContainsKey(name) ||
                !Design.Views[name].Equals(v))
            {
                CreateView(name, v);
            }
            return Queries[name];
        }

        private Query<TEntity> CreateView(string name, View v)
        {
            Design.Views[name] = v;
            if (!Queries.ContainsKey(name))
            {
                CreateQuery(name, v);
            }
            Session.Save(Design);
            return Queries[name];
        }

        private Query<TEntity> CreateQuery(string name, View v)
        {
            var q = new Query<TEntity>(
                Session,
                typeof(TEntity).Name.ToLowerInvariant(),
                name,
                !String.IsNullOrEmpty(v.Reduce)  
            );
            Queries.Add(name, q);
            return q;
        }

        public IPrimayOperator<TEntity, TKey> Where<TKey>(Expression<Func<TEntity, TKey>> xp)
        {
            return new PrimaryExpression<TKey>(
                new ExpressionValues
                    {
                        Repository = this,
                        Fields = new List<string> { BuildJavasriptExpressionFromLinq(xp.Body) }
                    }
            );
        }

        public Query<TEntity>.Spec All()
        {
            if (!Design.Views.ContainsKey("_all"))
            {
                var a = 
                    @"function(doc) {
                      if (doc._id.indexOf('" + typeof (TEntity).Name.ToLowerInvariant() + @"-') === 0) {
                        emit(null, null);
                      }
                    }";
                CreateView("_all", new View {Map = a});
            }
            return Queries["_all"].All();
        }
    }
}
