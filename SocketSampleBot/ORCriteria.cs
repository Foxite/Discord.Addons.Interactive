using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;

namespace SampleBot
{
    public class ORCriteria<T> : ICriterion<T>
    {
        /// <summary>
        /// The criteria.
        /// </summary>
        private readonly IEnumerable<ICriterion<T>> criteria;

        public ORCriteria(params ICriterion<T>[] criteria) {
            this.criteria = criteria;
        }

        public ORCriteria(IEnumerable<ICriterion<T>> criteria) {
            this.criteria = criteria;
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
                if (await criterion.JudgeAsync(sourceContext, parameter))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
