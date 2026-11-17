import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import CustomBackdrop from './components/Common/CustomBackdrop';
import AppLayout from './components/Layout/AppLayout';
import { RoutePath } from './constants/routes';
import i18n from './i18n';

// Each workstream replaces only its own view files; route registration stays here (Phase 0).
const SessionsListView = lazy(() => import('./views/Sessions/List'));
const SessionsCreateView = lazy(() => import('./views/Sessions/Create'));
const SessionsEditView = lazy(() => import('./views/Sessions/Edit'));
const SessionsViewView = lazy(() => import('./views/Sessions/View'));
const CentersListView = lazy(() => import('./views/Centers/List'));
const CentersCreateView = lazy(() => import('./views/Centers/Create'));
const CentersEditView = lazy(() => import('./views/Centers/Edit'));
const RoomsListView = lazy(() => import('./views/Rooms/List'));
const RoomsCreateView = lazy(() => import('./views/Rooms/Create'));
const RoomsEditView = lazy(() => import('./views/Rooms/Edit'));
const ParticipantsListView = lazy(() => import('./views/Participants/List'));
const ParticipantsImportView = lazy(() => import('./views/Participants/Import'));
const RulesListView = lazy(() => import('./views/Rules/List'));
const RulesCreateView = lazy(() => import('./views/Rules/Create'));
const RulesEditView = lazy(() => import('./views/Rules/Edit'));
const PinsListView = lazy(() => import('./views/Pins/List'));
const SolveRunsListView = lazy(() => import('./views/SolveRuns/List'));
const SolveRunsViewView = lazy(() => import('./views/SolveRuns/View'));
const AllocationsViewView = lazy(() => import('./views/Allocations/View'));
const AllocationsRoomView = lazy(() => import('./views/Allocations/Room'));

const AppRoutes = () => (
  <Suspense fallback={<CustomBackdrop label={i18n.t('loading')} />}>
    <Routes>
      <Route element={<AppLayout />}>
        <Route path={RoutePath.Home} element={<Navigate to={RoutePath.Sessions} replace />} />
        <Route path={RoutePath.Sessions} element={<SessionsListView />} />
        <Route path={RoutePath.SessionCreate} element={<SessionsCreateView />} />
        <Route path={RoutePath.SessionEdit} element={<SessionsEditView />} />
        <Route path={RoutePath.SessionView} element={<SessionsViewView />} />
        <Route path={RoutePath.Centers} element={<CentersListView />} />
        <Route path={RoutePath.CenterCreate} element={<CentersCreateView />} />
        <Route path={RoutePath.CenterEdit} element={<CentersEditView />} />
        <Route path={RoutePath.Rooms} element={<RoomsListView />} />
        <Route path={RoutePath.RoomCreate} element={<RoomsCreateView />} />
        <Route path={RoutePath.RoomEdit} element={<RoomsEditView />} />
        <Route path={RoutePath.Participants} element={<ParticipantsListView />} />
        <Route path={RoutePath.ParticipantsImport} element={<ParticipantsImportView />} />
        <Route path={RoutePath.Rules} element={<RulesListView />} />
        <Route path={RoutePath.RuleCreate} element={<RulesCreateView />} />
        <Route path={RoutePath.RuleEdit} element={<RulesEditView />} />
        <Route path={RoutePath.Pins} element={<PinsListView />} />
        <Route path={RoutePath.SolveRuns} element={<SolveRunsListView />} />
        <Route path={RoutePath.SolveRunView} element={<SolveRunsViewView />} />
        <Route path={RoutePath.AllocationView} element={<AllocationsViewView />} />
        <Route path={RoutePath.AllocationRoom} element={<AllocationsRoomView />} />
      </Route>
    </Routes>
  </Suspense>
);

export default AppRoutes;
