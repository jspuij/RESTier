using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class RestierConventionEntitySetDefinition : RestierConventionDefinition
    {

        #region Properties

        /// <summary>
        /// The name of the EntitySet associated with this ConventionDefinition.
        /// </summary>
        public string EntitySetName { get; set; }

        /// <summary>
        /// The Restier Operation associated with this ConventionDefinition.
        /// </summary>
        public RestierEntitySetOperation EntitySetOperation { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="RestierConventionEntitySetDefinition"/> instance.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pipelineState"></param>
        /// <param name="entitySetName"></param>
        /// <param name="entitySetOperation"></param>
        internal RestierConventionEntitySetDefinition(string name, RestierPipelineState pipelineState, string entitySetName, RestierEntitySetOperation entitySetOperation)
            : base(name, pipelineState)
        {
            EntitySetName = entitySetName;
            EntitySetOperation = entitySetOperation;
        }

        #endregion

    }

}
