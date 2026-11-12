<template>
  <section class="page-heading">
    <div>
      <nav>Home <span>›</span> My Cases</nav>
      <h1>My Cases</h1>
      <p>Cases in which you are directly involved will appear here.</p>
    </div>
  </section>

  <main class="content">
    <section class="card">
      <header>
        <div>
          <span>MY CASES</span>
          <h2>Assigned cases</h2>
        </div>
      </header>
      <div class="empty-state">
        <div>□</div>
        <h3>No cases to display</h3>
        <p>The case-list API has not been implemented in the current backend POC.</p>
      </div>
    </section>
  </main>
</template>

<style scoped>
.page-heading { background: var(--mint); border-bottom: 1px solid var(--border); }
.page-heading > div, .content { margin: 0 auto; max-width: 88rem; padding: 2rem 4rem; }
.page-heading nav { color: var(--muted); font-size: .78rem; margin-bottom: 1.4rem; }
.page-heading nav span { margin: 0 .5rem; }
.page-heading h1 { color: var(--ink); font-size: 2.5rem; margin: 0; }
.page-heading p { color: var(--muted); margin: .35rem 0 0; }
.card { background: white; border: 1px solid var(--border); border-radius: .5rem; overflow: hidden; }
.card header { background: var(--mint); border-bottom: 1px solid var(--border); padding: 1.3rem 1.6rem; }
.card header span { color: var(--brand); font-size: .7rem; font-weight: 750; letter-spacing: .13em; }
.card h2 { color: var(--ink); font-size: 1.15rem; margin: .2rem 0 0; }
.empty-state { color: var(--muted); padding: 5rem 2rem; text-align: center; }
.empty-state div { color: var(--brand); font-size: 2rem; }
.empty-state h3 { color: var(--ink); margin: 1rem 0 .4rem; }
.empty-state p { margin: 0; }
</style>
