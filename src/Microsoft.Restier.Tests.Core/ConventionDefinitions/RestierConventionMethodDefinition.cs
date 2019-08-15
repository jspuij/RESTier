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
    public class RestierConventionMethodDefinition : RestierConventionDefinition
    {

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RestierOperationMethod MethodOperation { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pipelineState"></param>
        /// <param name="methodName"></param>
        /// <param name="methodOperation"></param>
        public RestierConventionMethodDefinition(string name, RestierPipelineState pipelineState, string methodName, RestierOperationMethod methodOperation)
            : base(name, pipelineState)
        {
            MethodName = methodName;
            MethodOperation = methodOperation;
        }

        #endregion

    }

}
