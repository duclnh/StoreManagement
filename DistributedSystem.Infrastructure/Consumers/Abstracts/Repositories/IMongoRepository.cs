using System.Linq.Expressions;

namespace DistributedSystem.Infrastructure.Consumers.Abstract.Repositories
{
    public interface IMongoRepository<TDocument> where TDocument : IDocument
    {
        IQueryable<TDocument> AsQueryAble();

        IEnumerable<TDocument> FilterBy(
            Expression<Func<TDocument, bool>> filterExpression
        );

        IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression,
              Expression<Func<TDocument, TProjected>> projectedExpression
        );

        TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression);

        Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression);

        TDocument FindById(string id);

        Task<TDocument> FindByIdAsync(string id);

        void InsertOne(TDocument document);

        Task InsertOneAsync(TDocument document);

        void InsertMany(ICollection<TDocument> document);

        Task InsertManyAsync(ICollection<TDocument> document);

        void ReplaceOne(TDocument document);

        Task ReplaceOneAsync(TDocument document);

        void DeleteOne(Expression<Func<TDocument, bool>> filterExpression);

        Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression);

        void DeleteById(string id);

        Task DeleteByIdAsync(string id);

        void DeleteMany(Expression<Func<TDocument, bool>> filterExpression);
        
        Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression);

    }
}