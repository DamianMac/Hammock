// 
//  Repository.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Web;
using RedBranch.Hammock.Design;

namespace RedBranch.Hammock
{
    public interface IRepository<TEntity> where TEntity : class
    {
        TEntity Get(string id);
        Document Save(TEntity entity);
        void Delete(TEntity entity);
        IPrimayOperator<TEntity, TKey> Where<TKey>(Expression<Func<TEntity, TKey>> xp);
        Query<TEntity>.Spec All();

        void Attach(TEntity entity, string filename, HttpWebResponse response);
        void Attach(TEntity entity, HttpPostedFileBase file);
        void Attach(TEntity entity, string filename, string contentType, Stream data);
        void Attach(TEntity entity, string filename, string contentType, long contentLength, Stream data);
        TEntity TryGet(string id);
    }

    public partial class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        public Session Session { get; private set; }
        public DesignDocument Design { get; private set; }

        public IDictionary<string, Query<TEntity>> Queries { get; private set; }

        public Repository(Session sx) 
        {
            __init(sx, null);
        }

        public Repository(Session sx, DesignDocument design)
        {
            __init(sx, design);
        }

        private void __init(Session sx, DesignDocument design)
        {
            Queries = new Dictionary<string, Query<TEntity>>();
            Session = sx;

            Design = design;
            if (null == Design)
            {
                try
                {
                    Design = Session.Load<DesignDocument>(GetDesignDocumentName());
                }
                catch 
                {
                    Design = new DesignDocument();
                    Design.Language = "javascript";
                    Design.Views = new Dictionary<string, View>();
                    Session.Save(Design, GetDesignDocumentName());
                }
            }

            foreach (var v in Design.Views)
            {
                CreateQuery(v.Key, v.Value);
            }
        }

        private static string GetDesignDocumentName()
        {
            return "_design/" + typeof (TEntity).Name.ToLowerInvariant();
        }

        public TEntity Get(string id)
        {
            return Session.Load<TEntity>(id);
        }

        public TEntity TryGet(string id)
        {
            try
            {
                return Get(id);
            }
            catch
            {
                return null;
            }
        }

        public Document Save(TEntity entity)
        {
            return Session.Save(entity);
        }

        public void Delete(TEntity entity)
        {
            Session.Delete(entity);
        }

        public void Attach(TEntity entity, string filename, HttpWebResponse response)
        {
            Session.AttachFile(entity, filename, response);
        }
        public void Attach(TEntity entity, HttpPostedFileBase file)
        {
            Session.AttachFile(entity, file);
        }

        public void Attach(TEntity entity, string filename, string contentType, Stream data)
        {
            Session.AttachFile(entity, filename, contentType, data);
        }

        public void Attach(TEntity entity, string filename, string contentType, long contentLength, Stream data)
        {
            Session.AttachFile(entity, filename, contentType, contentLength, data);
        }
    }
}
