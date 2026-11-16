// UNTESTED. Progress is stored on the ACCOUNT via the game backend; this is only the client cache/interface.
using System.Threading.Tasks;
namespace NextPlay.Tutorial {
  public interface IAccountTutorialApi {            // backend implements: GET/PUT /accounts/{id}/tutorials/{tutorialId}
    Task<TutorialRecord> Get(string tutorialId); Task Put(string tutorialId, TutorialRecord r); Task<bool> IsNewAccount();
  }
  public class TutorialRecord { public bool completed, skipped; public int step; }
  public class TutorialProgressService {
    readonly IAccountTutorialApi api; public TutorialProgressService(IAccountTutorialApi a) { api = a; }
    public async Task<bool> ShouldAutoLaunchFirstLogin() { var r = await api.Get("first_login"); return await api.IsNewAccount() && (r == null || (!r.completed && !r.skipped)); }
    public Task Skip(string id) => api.Put(id, new TutorialRecord { skipped = true });
    public Task Complete(string id) => api.Put(id, new TutorialRecord { completed = true });
  }
}
