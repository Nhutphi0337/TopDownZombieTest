public interface IPoolable
{
    void SetPooler(IPooler pooler);

    void OnSpawned();

    void OnReleased();
}