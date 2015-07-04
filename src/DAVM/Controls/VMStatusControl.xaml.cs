using DAVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DAVM.Controls
{
	/// <summary>
	/// Interaction logic for VMStatusControl.xaml
	/// </summary>
	public partial class VMStatusControl : UserControl
	{
		public VMStatusControl()
		{
			InitializeComponent();
		}

		public static readonly DependencyProperty VMStatusProperty = DependencyProperty.Register("Status", typeof(ResourceStatus), typeof(VMStatusControl));
		public ResourceStatus Status
		{
			get { return (ResourceStatus)GetValue(VMStatusProperty); }
			set { SetValue(VMStatusProperty, value); }
		}
	}
}
