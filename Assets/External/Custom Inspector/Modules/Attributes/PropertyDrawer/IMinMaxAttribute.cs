namespace CustomInspector
{
    public interface IMinMaxAttribute
    {
        public abstract float CapValue { get; }
        public abstract string CapPath { get; }

        public abstract bool DependsOnOtherProperty();
    }
}
