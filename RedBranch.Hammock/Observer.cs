using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RedBranch.Hammock.Design;

namespace RedBranch.Hammock
{
        public enum Disposition
    {
        Continue,
        Decline,
    }

    public interface IObserver
    {
        Disposition BeforeSave(object entity, Document document);
        Disposition BeforeDelete(object entity, Document document);

        void AfterSave(object entity, Document document);
        void AfterDelete(object entity, Document document);
        void AfterLoad(object entity, Document document);
    }

    public abstract class BaseObserver : IObserver
    {
        public virtual Disposition BeforeSave(object entity, Document document)
        {
            return Disposition.Continue;
        }

        public virtual Disposition BeforeDelete(object entity, Document document)
        {
            return Disposition.Continue;
        }

        public virtual void AfterSave(object entity, Document document)
        {
        }

        public virtual void AfterDelete(object entity, Document document)
        {
        }

        public virtual void AfterLoad(object entity, Document document)
        {
        }
    }

    public abstract class BaseObserver<TEntity> : IObserver where TEntity : class
    {
        Disposition IObserver.BeforeSave(object entity, Document document)
        {
            return entity.GetType() == typeof(TEntity) 
                ? BeforeSave((TEntity) entity, document)
                : Disposition.Continue;
        }

        Disposition IObserver.BeforeDelete(object entity, Document document)
        {
             return entity.GetType() == typeof(TEntity) 
                ? BeforeDelete((TEntity) entity, document)
                : Disposition.Continue;
        }

        void IObserver.AfterSave(object entity, Document document)
        {
            if (entity.GetType() == typeof(TEntity))
                AfterSave((TEntity) entity, document);
        }

        void IObserver.AfterDelete(object entity, Document document)
        {
            if (entity.GetType() == typeof(TEntity))
                AfterDelete((TEntity) entity, document);
        }

        void IObserver.AfterLoad(object entity, Document document)
        {
            if (entity.GetType() == typeof(TEntity))
                AfterLoad((TEntity) entity, document);
        }

        public virtual Disposition BeforeSave(TEntity entity, Document document)
        {
            return Disposition.Continue;
        }

        public virtual Disposition BeforeDelete(TEntity entity, Document document)
        {
            return Disposition.Continue;
        }

        public virtual void AfterSave(TEntity entity, Document document)
        {
        }

        public virtual void AfterDelete(TEntity entity, Document document)
        {
        }

        public virtual void AfterLoad(TEntity entity, Document document)
        {
        }
    }
}