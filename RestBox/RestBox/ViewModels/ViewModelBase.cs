using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace RestBox.ViewModels
{
    public abstract class ViewModelBase<TViewModel> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        public void OnPropertyChanged(Expression<Func<TViewModel, object>> propertyName)
        {
            OnPropertyChanged(GetPropertyName(propertyName));
        }

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private static string GetPropertyName(Expression<Func<TViewModel, object>> expression)
        {
            var body = expression.Body as MemberExpression ?? ((UnaryExpression)expression.Body).Operand as MemberExpression;

            if (body == null)
            {
                throw new Exception("Unable to get property name");
            }

            return body.Member.Name;
        }
    }
}
