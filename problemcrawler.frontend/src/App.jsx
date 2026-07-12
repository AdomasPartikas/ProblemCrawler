import { Routes, Route, NavLink } from 'react-router-dom'
import Atlas from './pages/Atlas.jsx'
import Opportunities from './pages/Opportunities.jsx'
import Explorer from './pages/Explorer.jsx'
import Validation from './pages/Validation.jsx'
import './App.css'

export default function App() {
    return (
        <div className="app">
            <nav className="nav">
                <span className="nav-title">ProblemCrawler</span>
                <NavLink to="/" end className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>Atlas</NavLink>
                <NavLink to="/opportunities" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>Opportunities</NavLink>
                <NavLink to="/explorer" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>Explorer</NavLink>
                <NavLink to="/validation" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>Validation</NavLink>
            </nav>
            <main className="main">
                <Routes>
                    <Route path="/" element={<Atlas />} />
                    <Route path="/opportunities" element={<Opportunities />} />
                    <Route path="/explorer" element={<Explorer />} />
                    <Route path="/validation" element={<Validation />} />
                </Routes>
            </main>
        </div>
    )
}