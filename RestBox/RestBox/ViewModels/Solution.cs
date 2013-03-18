using System.Collections.Generic;

namespace RestBox.ViewModels
{
    public class Solution
    {
        #region Declarations
        
        static Solution solution = new Solution(); 

        #endregion

        #region Constructor
        
        public Solution()
        {
            HttpRequestFiles = new List<HttpRequestFile>();
            RequestEnvironmentFiles = new List<RequestEnvironmentFile>();
            RequestExtensionsFilePaths = new List<string>();
        } 

        #endregion

        #region Properties
        
        public string Name { get; set; }
        public string FilePath { get; set; }
        public List<HttpRequestFile> HttpRequestFiles { get; set; }
        public List<RequestEnvironmentFile> RequestEnvironmentFiles { get; set; }
        public List<string> RequestExtensionsFilePaths { get; set; }
        
        #endregion

        #region Properties

        public static Solution Current
        {
            get { return solution; }
            set { solution = value; }
        }

        public void Clear()
        {
            Name = null;
            FilePath = null;
            HttpRequestFiles.Clear();
            RequestEnvironmentFiles.Clear();
            RequestExtensionsFilePaths.Clear();
        } 

        #endregion
    }
}
