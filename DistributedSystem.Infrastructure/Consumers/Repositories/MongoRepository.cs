using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DistributedSystem.Infrastructure.Consumers.Abstract.Repositories;
using DistributedSystem.Infrastructure.Consumers.Attributes;
using DistributedSystem.Infrastructure.DependencyInjections.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DistributedSystem.Infrastructure.Consumers.Repositories
{
    public class MongoRepository<TDocument> : IMongoRepository<TDocument>
    where TDocument : IDocument
    {
        private readonly IMongoCollection<TDocument> _mongoCollection;

        public MongoRepository(IMongoDbOptions mongoDbOptions)
        {
            var database = new MongoClient(
                mongoDbOptions.DefaultConnectString
            ).GetDatabase(mongoDbOptions.DatabaseName);
            _mongoCollection = database.GetCollection<TDocument>(GetCollectionName(typeof(TDocument)));
        }

        public virtual IQueryable<TDocument> AsQueryAble()
        {
            return _mongoCollection.AsQueryable();
        }

        public virtual void DeleteById(string id)
        {
            var objectId = new ObjectId(id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            _mongoCollection.FindOneAndDelete(filter);
        }

        public virtual Task DeleteByIdAsync(string id)
        {
            return Task.Run(() =>
             {
                 var objectId = new ObjectId(id);
                 var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
                 _mongoCollection.FindOneAndDeleteAsync(filter);
             });
        }

        public virtual void DeleteMany(Expression<Func<TDocument, bool>> filterExpression)
        {
            _mongoCollection.DeleteMany(filterExpression);
        }

        public virtual Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() => _mongoCollection.DeleteManyAsync(filterExpression));
        }

        public virtual void DeleteOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            _mongoCollection.DeleteOne(filterExpression);
        }

        public virtual Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() => _mongoCollection.DeleteOneAsync(filterExpression));
        }

        public virtual IEnumerable<TDocument> FilterBy(Expression<Func<TDocument, bool>> filterExpression)
        {
            return _mongoCollection.Find(filterExpression).ToEnumerable();
        }

        public virtual IEnumerable<TProjected> FilterBy<TProjected>(Expression<Func<TDocument, bool>> filterExpression, Expression<Func<TDocument, TProjected>> projectedExpression)
        {
            return _mongoCollection.Find(filterExpression).Project(projectedExpression).ToEnumerable();
        }

        public virtual TDocument FindById(string id)
        {
            var objectId = new ObjectId(id);
            return _mongoCollection.Find(x => x.Id.Equals(objectId)).Single();
        }
        public virtual Task<TDocument> FindByIdAsync(string id)
        {
            return Task.Run(() =>
            {
                var objectId = new ObjectId(id);
                var result = _mongoCollection.Find(x => x.Id == objectId).FirstOrDefault();
                return result;
            });
        }
        public virtual TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            return _mongoCollection.Find(filterExpression).FirstOrDefault();
        }

        public virtual Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() => _mongoCollection.Find(filterExpression).FirstOrDefaultAsync());
        }

        public virtual void InsertMany(ICollection<TDocument> document)
        {
            _mongoCollection.InsertMany(document);
        }

        public virtual Task InsertManyAsync(ICollection<TDocument> document)
        {

            return Task.Run(() => _mongoCollection.InsertManyAsync(document));
        }

        public virtual void InsertOne(TDocument document)
        {
            _mongoCollection.InsertOne(document);
        }

        public virtual Task InsertOneAsync(TDocument document)
        {
            return Task.Run(() => _mongoCollection.InsertOneAsync(document));
        }

        public virtual void ReplaceOne(TDocument document)
        {
            _mongoCollection.ReplaceOne(x => x.Id == document.Id, document);
        }

        public virtual Task ReplaceOneAsync(TDocument document)
        {
            return Task.Run(() => _mongoCollection.ReplaceOneAsync(x => x.Id == document.Id, document));
        }

        private protected string GetCollectionName(Type documentType)
        {
            return ((BsonCollectionAttribute)documentType
                .GetCustomAttributes(typeof(BsonCollectionAttribute), true).FirstOrDefault())?.CollectionName;
        }


    }
}