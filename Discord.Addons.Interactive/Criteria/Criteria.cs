﻿// ReSharper disable StyleCop.SA1600
namespace Discord.Addons.Interactive
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord.Commands;

    public class Criteria<T> : ICriterion<T>
    {
        /// <summary>
        /// The criteria.
        /// </summary>
        private readonly List<ICriterion<T>> criteria;

        public Criteria(params ICriterion<T>[] criteria) {
            this.criteria = criteria.ToList();
        }

        public Criteria(IEnumerable<ICriterion<T>> criteria) {
            this.criteria = criteria.ToList();
        }

        public Criteria<T> AddCriterion(ICriterion<T> criterion) {
            criteria.Add(criterion);
            return this;
        }

        /// <summary>
        /// The judge async.
        /// </summary>
        /// <param name="sourceContext">
        /// The source context.
        /// </param>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<bool> JudgeAsync(SocketCommandContext sourceContext, T parameter)
        {
            foreach (var criterion in criteria)
            {
                var result = await criterion.JudgeAsync(sourceContext, parameter).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
