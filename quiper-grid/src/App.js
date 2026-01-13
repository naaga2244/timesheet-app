import AppRoutes from "./routes/AppRoutes";
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap/dist/js/bootstrap.bundle.min.js";
import "primereact/resources/themes/lara-light-blue/theme.css"; // choose any theme
import "primereact/resources/primereact.min.css";
import "primeicons/primeicons.css";
import "primeflex/primeflex.css"; // optional for flex/grid utilities

function App() {

  return (
    <div className="App">
      <AppRoutes />
    </div>
  );
}

export default App;
