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
            HttpRequestFiles = new List<File>();
            RequestEnvironmentFiles = new List<File>();
            RequestExtensionsFilePaths = new List<string>();
            HttpRequestSequenceFiles = new List<File>();
        } 

        #endregion

        #region Properties
        
        public string Name { get; set; }
        public string FilePath { get; set; }
        public List<File> HttpRequestFiles { get; set; }
        public List<File> RequestEnvironmentFiles { get; set; }
        public List<File> HttpRequestSequenceFiles { get; set; }
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
