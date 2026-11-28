using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace FlexibleHMI.Templates;

public sealed record ViewModelChoice(string Label, ObservableObject ViewModel);

public static class ViewRegistrationExtensions
{
}
